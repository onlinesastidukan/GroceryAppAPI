# Firebase Backend Implementation - Completion Guide

## What's Been Done

✅ Created `NotificationService.cs` - FCM notification sender using Firebase Admin SDK (V1 API)
✅ Updated `User.cs` model - added `FcmToken` field
✅ Created `UsersController.cs` - endpoint to register FCM tokens
✅ Updated `OrdersController.cs` - sends FCM notifications when orders are created
✅ Registered `INotificationService` in `Program.cs`

**Note**: This implementation uses **Firebase Cloud Messaging API (V1)** via Firebase Admin SDK, NOT the deprecated Legacy API.

## Required Manual Steps

### 1. Install FirebaseAdmin NuGet Package

**Option A: Package Manager Console**
```powershell
cd E:\Rohit_Mundhe\WOrk\SastiDukan\GroceryAppAPI
Install-Package FirebaseAdmin
```

**Option B: Visual Studio UI**
1. Right-click on GroceryAppAPI project → Manage NuGet Packages
2. Search for `FirebaseAdmin`
3. Install the latest version

### 2. Add Firebase Service Account JSON

1. Follow steps in `FIREBASE_FCM_SETUP_GUIDE.md` (Part 5.2) to download service account JSON from Firebase Console
2. Create a folder `Firebase` in your backend project root:
   ```
   E:\Rohit_Mundhe\WOrk\SastiDukan\GroceryAppAPI\Firebase\
   ```
3. Copy the downloaded JSON file (e.g., `groceryapp-firebase-adminsdk-xxxxx.json`) into this folder
4. Rename it to `firebase-adminsdk.json` for simplicity
5. Right-click the file in Visual Studio → Properties
6. Set **Copy to Output Directory**: `Copy if newer`

### 3. Update appsettings.json

Add Firebase configuration to `appsettings.json`:

```json
{
  "ConnectionStrings": {
	"DefaultConnection": "..."
  },
  "Jwt": {
	...
  },
  "Firebase": {
	"ProjectId": "your-firebase-project-id",
	"ServiceAccountPath": "Firebase/firebase-adminsdk.json"
  }
}
```

Replace `your-firebase-project-id` with your actual Firebase project ID from the Firebase Console.

### 4. Create Database Migration for FcmToken

Run this command in Package Manager Console:

```powershell
Add-Migration AddFcmTokenToUsers
Update-Database
```

Or if using EF Core CLI:
```bash
dotnet ef migrations add AddFcmTokenToUsers
dotnet ef database update
```

This will add the `fcm_token` column to the `users` table.

### 5. Test the Implementation

#### Test 1: Register FCM Token
1. Login as a shopkeeper from mobile app
2. Check backend logs - should see "FCM token registered for user X"
3. Verify token is stored in database:
   ```sql
   SELECT id, full_name, fcm_token FROM users WHERE role_id = 3;
   ```

#### Test 2: Send Test Notification
1. Place an order from customer app
2. Check backend logs - should see "FCM notification sent successfully"
3. Shopkeeper device should receive a push notification

#### Test 3: Firebase Console Test
1. Go to Firebase Console → Cloud Messaging → Send your first message
2. Copy a shopkeeper's FCM token from database
3. Click "Send test message"
4. Paste token and send
5. Shopkeeper device should receive notification

## Flow Diagram

```
Customer Places Order
	↓
Backend Creates Order
	↓
CreateDealerNotificationsAsync
	↓
	├─→ Create DB Notification (for in-app)
	↓
	└─→ NotificationService.SendOrderNotificationAsync
			↓
			├─→ Get Dealer's FCM Token from DB
			↓
			├─→ Build FCM Message
			↓
			└─→ Firebase Admin SDK sends to device
					↓
					Device receives notification
					↓
					MyFirebaseMessagingService displays it
```

## API Endpoints

### Register FCM Token
```http
POST /api/users/fcm-token
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
  "fcmToken": "device_token_here"
}
```

Response:
```json
{
  "success": true,
  "message": "FCM token registered successfully"
}
```

### Create Order (automatically sends notification)
```http
POST /api/orders
Content-Type: application/json

{
  "customerName": "John Doe",
  "mobileNumber": "9876543210",
  "deliveryAddress": "123 Main St",
  "items": [
	{ "productId": 1, "quantity": 2 }
  ]
}
```

## Troubleshooting

### "Firebase not initialized"
- Check that `firebase-adminsdk.json` exists in `Firebase` folder
- Verify `appsettings.json` has correct path
- Check file Build Action is set to "Copy if newer"

### "FCM token is invalid or unregistered"
- Token may have expired or device uninstalled app
- Mobile app should re-register token on next login
- Check logs for specific Firebase error codes

### No notification received
- Verify FCM token is stored in database for that user
- Check backend logs for "FCM notification sent successfully"
- Ensure mobile app has notification permissions
- Test with Firebase Console test message first

### "Dealer has no FCM token registered"
- Shopkeeper needs to login from mobile app first
- Token is registered during login (see mobile implementation)
- Check `fcm_token` column in users table

## Database Schema

The `users` table now includes:

```sql
CREATE TABLE users (
	id SERIAL PRIMARY KEY,
	user_id VARCHAR(50) NOT NULL,
	full_name VARCHAR(100) NOT NULL,
	mobile_number VARCHAR(20) NOT NULL,
	address TEXT,
	password_hash TEXT NOT NULL,
	role_id INTEGER NOT NULL,
	created_by INTEGER,
	created_at TIMESTAMP DEFAULT NOW(),
	updated_at TIMESTAMP,
	is_active BOOLEAN DEFAULT TRUE,
	fcm_token TEXT  -- NEW COLUMN
);
```

## Security Considerations

1. **Service Account JSON**: Keep this file VERY secure
   - Never commit to Git
   - Add to `.gitignore`
   - Treat like a password

2. **FCM Tokens**: Sensitive data
   - Only store tokens for logged-in users
   - Clear tokens on logout (optional)
   - Handle invalid tokens gracefully

3. **Notification Content**: Don't include sensitive data
   - Order totals: OK
   - Customer payment info: NO
   - Personal details: Minimal

## Cost & Limits

- **FCM Messages**: Completely FREE, unlimited
- **Firebase Admin SDK**: FREE
- **Database storage**: Tokens are ~150-200 bytes each
  - 1000 tokens ≈ 200 KB storage

## Next Steps

After completing these manual steps:

1. ✅ Test FCM token registration from mobile app
2. ✅ Place a test order and verify notification received
3. ✅ Check database for stored FCM tokens
4. ✅ Review backend logs for any errors
5. ✅ Test on multiple shopkeeper devices
6. ✅ Generate fresh mobile APK with all changes
7. ✅ Deploy backend to Railway/production

## Support

If you encounter issues:
- Check backend logs in Railway dashboard
- Use Firebase Console → Cloud Messaging → Debug
- Verify database migrations were applied
- Test with simple console app first if needed

---

**Backend FCM Implementation Complete!** 🎉

Once manual steps 1-4 are done, your backend will send push notifications to shopkeepers when orders are placed.
