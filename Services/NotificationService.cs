using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GroceryOrderingApp.Backend.Repositories;

namespace GroceryOrderingApp.Backend.Services
{
    public interface INotificationService
    {
        Task<bool> SendOrderNotificationAsync(int dealerId, int orderId, string customerName, decimal totalAmount);
        Task<bool> SendNotificationToTokenAsync(string fcmToken, string title, string body, Dictionary<string, string>? data = null);
    }

    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IUserRepository _userRepository;
        private bool _firebaseInitialized = false;

        public NotificationService(
            ILogger<NotificationService> logger,
            IConfiguration configuration,
            IUserRepository userRepository)
        {
            _logger = logger;
            _configuration = configuration;
            _userRepository = userRepository;
            InitializeFirebase();
        }

        private void InitializeFirebase()
        {
            try
            {
                if (FirebaseApp.DefaultInstance != null)
                {
                    _firebaseInitialized = true;
                    return;
                }

                GoogleCredential? credential = TryGetCredentialFromGoogleApplicationCredentials();
                if (credential == null)
                {
                    credential = TryGetCredentialFromServiceAccountJson();
                }

                if (credential == null)
                {
                    credential = TryGetCredentialFromConfiguredFilePath();
                }

                if (credential == null)
                {
                    _logger.LogWarning("Firebase credentials were not found. Configure GOOGLE_APPLICATION_CREDENTIALS (file path) or FIREBASE_SERVICE_ACCOUNT_JSON.");
                    return;
                }

                FirebaseApp.Create(new AppOptions
                {
                    Credential = credential
                });

                _firebaseInitialized = true;
                _logger.LogInformation("Firebase Admin SDK initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Firebase Admin SDK");
            }
        }

        private GoogleCredential? TryGetCredentialFromGoogleApplicationCredentials()
        {
            var path = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            if (!File.Exists(path))
            {
                _logger.LogWarning("GOOGLE_APPLICATION_CREDENTIALS is set but file does not exist at: {Path}", path);
                return null;
            }

            _logger.LogInformation("Using Firebase credentials from GOOGLE_APPLICATION_CREDENTIALS.");
            return GoogleCredential.FromFile(path);
        }

        private GoogleCredential? TryGetCredentialFromServiceAccountJson()
        {
            var serviceAccountJson =
                Environment.GetEnvironmentVariable("FIREBASE_SERVICE_ACCOUNT_JSON")
                ?? _configuration["Firebase:ServiceAccountJson"];

            if (string.IsNullOrWhiteSpace(serviceAccountJson))
            {
                return null;
            }

            _logger.LogInformation("Using Firebase credentials from FIREBASE_SERVICE_ACCOUNT_JSON.");
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(serviceAccountJson));
            return GoogleCredential.FromStream(stream);
        }

        private GoogleCredential? TryGetCredentialFromConfiguredFilePath()
        {
            var configuredPath = _configuration["Firebase:ServiceAccountPath"];
            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                _logger.LogWarning("Firebase:ServiceAccountPath is not configured.");
                return null;
            }

            var candidatePaths = new[]
            {
                configuredPath,
                Path.Combine(AppContext.BaseDirectory, configuredPath),
                Path.Combine(Directory.GetCurrentDirectory(), configuredPath)
            };

            foreach (var path in candidatePaths)
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                _logger.LogInformation("Using Firebase credentials from file path: {Path}", path);
                return GoogleCredential.FromFile(path);
            }

            _logger.LogWarning("Firebase service account file not found. Checked paths: {Paths}", string.Join(", ", candidatePaths));
            return null;
        }

        public async Task<bool> SendOrderNotificationAsync(int dealerId, int orderId, string customerName, decimal totalAmount)
        {
            if (!_firebaseInitialized)
            {
                _logger.LogWarning("Firebase not initialized, skipping notification");
                return false;
            }

            try
            {
                // Get dealer's FCM token from database
                var dealer = await _userRepository.GetUserByIdAsync(dealerId);
                if (dealer == null || string.IsNullOrEmpty(dealer.FcmToken))
                {
                    _logger.LogWarning($"Dealer {dealerId} has no FCM token registered");
                    return false;
                }

                var title = "New Order Received!";
                var body = $"Order #{orderId} from {customerName ?? "Customer"} - ₹{totalAmount:F0}";

                var data = new Dictionary<string, string>
                {
                    { "type", "new_order" },
                    { "orderId", orderId.ToString() },
                    { "totalAmount", totalAmount.ToString("F2") }
                };

                return await SendNotificationToTokenAsync(dealer.FcmToken, title, body, data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send order notification to dealer {dealerId}");
                return false;
            }
        }

        public async Task<bool> SendNotificationToTokenAsync(string fcmToken, string title, string body, Dictionary<string, string>? data = null)
        {
            if (!_firebaseInitialized)
            {
                _logger.LogWarning("Firebase not initialized, skipping notification");
                return false;
            }

            if (string.IsNullOrWhiteSpace(fcmToken))
            {
                _logger.LogWarning("FCM token is empty, skipping notification");
                return false;
            }

            try
            {
                var message = new Message()
                {
                    Token = fcmToken,
                    Notification = new Notification
                    {
                        Title = title,
                        Body = body
                    },
                    Data = data,
                    Android = new AndroidConfig
                    {
                        Priority = Priority.High,
                        Notification = new AndroidNotification
                        {
                            Sound = "default",
                            ChannelId = "order_notifications"
                        }
                    }
                };

                var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                _logger.LogInformation($"FCM notification sent successfully. Response: {response}");
                return true;
            }
            catch (FirebaseMessagingException fex)
            {
                _logger.LogError(fex, $"Firebase messaging error. Error code: {fex.MessagingErrorCode}");

                // If token is invalid, we should remove it from database
                if (fex.MessagingErrorCode == MessagingErrorCode.Unregistered ||
                    fex.MessagingErrorCode == MessagingErrorCode.InvalidArgument)
                {
                    _logger.LogWarning($"FCM token is invalid or unregistered: {fcmToken}");
                    // TODO: Mark token as invalid in database
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send FCM notification");
                return false;
            }
        }
    }
}
