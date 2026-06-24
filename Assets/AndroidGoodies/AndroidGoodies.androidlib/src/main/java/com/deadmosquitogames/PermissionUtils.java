package com.deadmosquitogames;

import android.app.Activity;
import android.content.Intent;
import androidx.core.app.ActivityCompat;
import android.util.Log;

import com.deadmosquitogames.util.Constants;
import com.deadmosquitogames.util.JsonUtil;
import com.deadmosquitogames.util.UnityUtil;

final class PermissionUtils {

	private static final String EXTRAS_PERMISSIONS = "EXTRAS_PERMISSIONS";

	private PermissionUtils() {
	}

	static void requestPermissions(Intent data, Activity activity, int requestCode) {
		String[] permissions = data.getStringArrayExtra(EXTRAS_PERMISSIONS);
		ActivityCompat.requestPermissions(activity, permissions, requestCode);
	}

	static void handleRequestPermissionsResult(AndroidGoodiesActivity activity, String[] permissions, int[] grantResults) {
		String json = JsonUtil.serializePermissionResults(activity, permissions, grantResults);
		Log.d(Constants.LOG_TAG, "Permission results: " + json);
		UnityUtil.onRequestPermissionsResult(json);
	}
}
