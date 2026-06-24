package com.deadmosquitogames.notifications;

import android.app.AlarmManager;
import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.pm.ApplicationInfo;
import android.content.res.Resources;
import android.graphics.BitmapFactory;
import android.graphics.Color;
import android.media.RingtoneManager;
import android.os.Build;
import androidx.core.app.NotificationCompat;
import androidx.core.app.TaskStackBuilder;

@SuppressWarnings("unused")
public class GoodiesNotificationManager extends BroadcastReceiver {

	private static final String TICKER = "ticker";
	private static final String TITLE = "title";
	private static final String MESSAGE = "message";
	private static final String ID = "id";
	private static final String COLOR = "color";
	private static final String SOUND = "sound";
	private static final String VIBRATE = "vibrate";
	private static final String LIGHTS = "lights";
	private static final String LARGE_ICON = "l_icon";
	private static final String SMALL_ICON = "s_icon";
	private static final String BUNDLE = "bundle";

	public static void setNotification(Context currentActivity,
									   int id,
									   long delayMs,
									   String title, String message, String ticker,
									   int sound, int vibrate, int lights,
									   String largeIconResource, String smallIconResource,
									   int bgColor, String bundle) {
		AlarmManager am = (AlarmManager) currentActivity.getSystemService(Context.ALARM_SERVICE);
		Intent intent = new Intent(currentActivity, GoodiesNotificationManager.class);
		intent.putExtra(TICKER, ticker);
		intent.putExtra(TITLE, title);
		intent.putExtra(MESSAGE, message);
		intent.putExtra(ID, id);
		intent.putExtra(COLOR, bgColor);
		intent.putExtra(SOUND, sound == 1);
		intent.putExtra(VIBRATE, vibrate == 1);
		intent.putExtra(LIGHTS, lights == 1);
		intent.putExtra(LARGE_ICON, largeIconResource);
		intent.putExtra(SMALL_ICON, smallIconResource);
		intent.putExtra(BUNDLE, bundle);
		PendingIntent broadcast = PendingIntent.getBroadcast(currentActivity, id, intent, PendingIntent.FLAG_UPDATE_CURRENT);
		if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
			am.setExact(AlarmManager.RTC_WAKEUP, System.currentTimeMillis() + delayMs, broadcast);
		} else {
			am.set(AlarmManager.RTC_WAKEUP, System.currentTimeMillis() + delayMs, broadcast);
		}
	}

	public static void setRepeatingNotification(Context currentActivity,
												int id,
												long delayMs, String title, String message, String ticker,
												long rep,
												int sound, int vibrate, int lights,
												String largeIconResource, String smallIconResource, int bgColor, String bundle) {
		AlarmManager am = (AlarmManager) currentActivity.getSystemService(Context.ALARM_SERVICE);
		Intent intent = new Intent(currentActivity, GoodiesNotificationManager.class);
		intent.putExtra(TICKER, ticker);
		intent.putExtra(TITLE, title);
		intent.putExtra(MESSAGE, message);
		intent.putExtra(ID, id);
		intent.putExtra(COLOR, bgColor);
		intent.putExtra(SOUND, sound == 1);
		intent.putExtra(VIBRATE, vibrate == 1);
		intent.putExtra(LIGHTS, lights == 1);
		intent.putExtra(LARGE_ICON, largeIconResource);
		intent.putExtra(SMALL_ICON, smallIconResource);
		intent.putExtra(BUNDLE, bundle);
		am.setRepeating(AlarmManager.RTC_WAKEUP, System.currentTimeMillis() + delayMs, rep, PendingIntent.getBroadcast(currentActivity, id, intent, PendingIntent.FLAG_UPDATE_CURRENT));
	}

	public static void cancelNotification(Context activity, int id) {
		AlarmManager am = (AlarmManager) activity.getSystemService(Context.ALARM_SERVICE);
		Intent intent = new Intent(activity, GoodiesNotificationManager.class);
		PendingIntent pendingIntent = PendingIntent.getBroadcast(activity, id, intent, 0);
		am.cancel(pendingIntent);
	}

	private static String getApplicationName(Context context) {
		ApplicationInfo applicationInfo = context.getApplicationInfo();
		int stringId = applicationInfo.labelRes;
		return stringId == 0 ? applicationInfo.nonLocalizedLabel.toString() : context.getString(stringId);
	}

	@Override
	public void onReceive(Context context, Intent intent) {
		NotificationManager notificationManager = (NotificationManager) context.getSystemService(Context.NOTIFICATION_SERVICE);

		String ticker = intent.getStringExtra(TICKER);
		String title = intent.getStringExtra(TITLE);
		String message = intent.getStringExtra(MESSAGE);
		String s_icon = intent.getStringExtra(SMALL_ICON);
		String l_icon = intent.getStringExtra(LARGE_ICON);
		int color = intent.getIntExtra(COLOR, 0);
		String bundle = intent.getStringExtra(BUNDLE);
		Boolean sound = intent.getBooleanExtra(SOUND, false);
		Boolean vibrate = intent.getBooleanExtra(VIBRATE, false);
		Boolean lights = intent.getBooleanExtra(LIGHTS, false);
		int id = intent.getIntExtra(ID, 0);

		Resources res = context.getResources();

		Intent notificationIntent = context.getPackageManager().getLaunchIntentForPackage(bundle);

		TaskStackBuilder stackBuilder = TaskStackBuilder.create(context);
		stackBuilder.addNextIntent(notificationIntent);

		PendingIntent pendingIntent = PendingIntent.getActivity(context, 0,
				notificationIntent, PendingIntent.FLAG_UPDATE_CURRENT);

		NotificationCompat.Builder builder = new NotificationCompat.Builder(context);

		builder.setContentIntent(pendingIntent)
				.setAutoCancel(true)
				.setContentTitle(title)
				.setContentText(message);

		if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP) {
			builder.setColor(color);
		}

		if (ticker != null && ticker.length() > 0) {
			builder.setTicker(ticker);
		}

		if (s_icon != null && s_icon.length() > 0) {
			builder.setSmallIcon(res.getIdentifier(s_icon, "drawable", context.getPackageName()));
		}

		if (l_icon != null && l_icon.length() > 0) {
			builder.setLargeIcon(BitmapFactory.decodeResource(res, res.getIdentifier(l_icon, "drawable", context.getPackageName())));
		}

		if (sound) {
			builder.setSound(RingtoneManager.getDefaultUri(RingtoneManager.TYPE_NOTIFICATION));
		}

		if (vibrate) {
			builder.setVibrate(new long[]{1000L, 1000L});
		}

		if (lights) {
			builder.setLights(Color.GREEN, 3000, 3000);
		}

		if (android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.O) {
			final String channelId = "default";
			NotificationChannel channel = new NotificationChannel(channelId, getApplicationName(context), NotificationManager.IMPORTANCE_LOW);
			notificationManager.createNotificationChannel(channel);

			builder.setChannelId(channelId);
		}

		Notification notification = builder.build();
		notificationManager.notify(id, notification);
	}

}
