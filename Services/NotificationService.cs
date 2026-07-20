using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GroceryOrderingApp.Backend.Repositories;

namespace GroceryOrderingApp.Backend.Services
{
    public interface INotificationService
    {
        Task<bool> SendOrderNotificationAsync(int dealerId, int orderId, string customerName, decimal totalAmount);
        Task<bool> SendNotificationToTokenAsync(string fcmToken, string title, string body, Dictionary<string, string> data = null);
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
                if (FirebaseApp.DefaultInstance == null)
                {
                    var serviceAccountPath = _configuration["Firebase:ServiceAccountPath"];

                    if (string.IsNullOrEmpty(serviceAccountPath))
                    {
                        _logger.LogWarning("Firebase ServiceAccountPath not configured in appsettings.json");
                        return;
                    }

                    if (!File.Exists(serviceAccountPath))
                    {
                        _logger.LogWarning($"Firebase service account file not found at: {serviceAccountPath}");
                        return;
                    }

                    FirebaseApp.Create(new AppOptions()
                    {
                        Credential = GoogleCredential.FromFile(serviceAccountPath)
                    });

                    _firebaseInitialized = true;
                    _logger.LogInformation("Firebase Admin SDK initialized successfully");
                }
                else
                {
                    _firebaseInitialized = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Firebase Admin SDK");
            }
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

        public async Task<bool> SendNotificationToTokenAsync(string fcmToken, string title, string body, Dictionary<string, string> data = null)
        {
            if (!_firebaseInitialized)
            {
                _logger.LogWarning("Firebase not initialized, skipping notification");
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
