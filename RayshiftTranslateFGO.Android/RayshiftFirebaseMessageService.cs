﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.Work;
using Firebase.Messaging;
using RayshiftTranslateFGO.Services;
using Xamarin.Essentials;

namespace RayshiftTranslateFGO.Droid
{
    [Service]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    [IntentFilter(new[] { "com.google.firebase.INSTANCE_ID_EVENT" })]
    public class RayshiftFirebaseMessagingService : FirebaseMessagingService
    {
        const string TAG = "RayshiftFirebaseMsgService";

        public override void OnMessageReceived(RemoteMessage message)
        {
            //Log.Debug(TAG, "From: " + message.From);
            //var body = message.GetNotification().Body; 
            //Log.Debug(TAG, "Notification Message Body: " + body);
            Log.Info(TAG, $"Received a firebase request from {message.From}.");
            if (message.From == $"/topics/{MainActivity.CHANNEL_ID}")
            {
                Log.Debug(TAG, "Received an announcement.");
                SendNotification(message.GetNotification().Body, message.GetNotification().Title, message.Data);
            }
            else if (message.From == $"/topics/{MainActivity.UPDATE_CHANNEL_NAME}" && 
                     !Preferences.ContainsKey("DisableAutoUpdate") && 
                     Preferences.Get("InstalledScript", null) != null)
            {
                Log.Debug(TAG, "Received an update request.");

                OneTimeWorkRequest request = OneTimeWorkRequest.Builder.From<RayshiftTranslationUpdateWorker>().Build();
                WorkManager.Instance.Enqueue(request);
            }
        }
        void SendNotification(string messageBody, string messageTitle, IDictionary<string, string> data)
        {
            var intent = new Intent(this, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.ClearTop);
            foreach (var key in data.Keys)
            {
                intent.PutExtra(key, data[key]);
            }

            var pendingIntent = PendingIntent.GetActivity(this,
                MainActivity.NOTIFICATION_ID,
                intent,
                PendingIntentFlags.OneShot);

            var notificationBuilder = new NotificationCompat.Builder(this, MainActivity.CHANNEL_ID)
                .SetSmallIcon(Resource.Drawable.ic_stat_ic_notification)
                .SetContentTitle(messageTitle)
                .SetContentText(messageBody)
                .SetAutoCancel(true)
                .SetContentIntent(pendingIntent);

            var notificationManager = NotificationManagerCompat.From(this);
            notificationManager.Notify(MainActivity.NOTIFICATION_ID, notificationBuilder.Build());
        }

        public override void OnNewToken(string token)
        {
#if DEBUG
            Log.Debug(TAG, "FCM token: " + token);
#endif
            SendRegistrationToServer(token);
        }

        async void SendRegistrationToServer(string token)
        {
            var rest = new RestfulAPI();
            await rest.SendRegistrationToken(token);
        }
    }
}