package com.deadmosquitogames.notifications;

import android.app.Notification;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;

import androidx.annotation.DrawableRes;
import androidx.annotation.Keep;
import androidx.core.app.NotificationManagerCompat;

import com.deadmosquitogames.R;
import com.deadmosquitogames.util.UnityUtil;
import com.unity3d.player.UnityPlayer;

@Keep
public class NotificationManagerModern extends BroadcastReceiver {
	@DrawableRes
	public static int getNotificationIconRes() {
		return R.drawable.notify_icon_small;
	}

	@Override
	public void onReceive(Context context, Intent intent) {
		Notification notification = intent.getParcelableExtra("notification");
		int id = intent.getIntExtra("id", 0);
		String tag = intent.getStringExtra("tag");

		NotificationManagerCompat.from(context).notify(tag, id, notification);
		UnityUtil.onNotificationReceived(id);
	}
}
