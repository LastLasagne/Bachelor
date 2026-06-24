package com.deadmosquitogames.util;

import android.util.Log;
import com.deadmosquitogames.multipicker.api.Picker;
import com.unity3d.player.UnityPlayer;

public class UnityUtil {

	private static final String GOODIES_UNITY_SCENE_HELPER_GAMEOBJECT_NAME = "GoodiesSceneHelper";

	private UnityUtil() {
	}

	public static void onContactPickSuccess(String json) {
		SendMessage("OnPickContactSuccess", json);
	}

	public static void onContactPickError(String message) {
		SendMessage("OnPickContactError", message);
	}

	public static void onPickGalleryPhotoError(String message) {
		SendMessage("OnPickGalleryImageError", message);
	}

	public static void onPickGalleryImageSuccess(String json) {
		SendMessage("OnPickGalleryImageSuccess", json);
	}

	public static void onPickMultipleGalleryImagesSuccess(String json) {
		SendMessage("OnPickMultipleGalleryImages", json);
	}

	public static void onTakePhotoError(String message) {
		SendMessage("OnPickPhotoImageError", message);
	}

	public static void onTakePhotoSuccess(String json) {
		SendMessage("OnPickPhotoImageSuccess", json);
	}

	public static void onRequestPermissionsResult(String json) {
		SendMessage("OnRequestPermissionsResult", json);
	}

	public static void onPickAudioError(String message) {
		SendMessage("OnPickAudioError", message);
	}

	public static void onPickAudioSuccess(String json) {
		SendMessage("OnPickAudioSuccess", json);
	}

	private static void SendMessage(String method, String message) {
		Log.d(Constants.LOG_TAG, "Sending message to Unity: " + message);
		UnityPlayer.UnitySendMessage(GOODIES_UNITY_SCENE_HELPER_GAMEOBJECT_NAME, method, message);
	}

	public static void onVideoSuccess(String message, int type) {
		if (type == Picker.PICK_VIDEO_DEVICE) {
			onPickVideoSuccess(message);
		} else if (type == Picker.PICK_VIDEO_CAMERA) {
			onRecordVideoSuccess(message);
		}

		Log.e(Constants.LOG_TAG, "Unexpected video picker type");
	}

	public static void onVideoError(String message, int type) {
		if (type == Picker.PICK_VIDEO_DEVICE) {
			onPickVideoError(message);
		} else if (type == Picker.PICK_VIDEO_CAMERA) {
			onRecordVideoError(message);
		}

		Log.e(Constants.LOG_TAG, "Unexpected video picker type");
	}

	private static void onPickVideoError(String message) {
		SendMessage("OnPickVideoError", message);
	}

	private static void onPickVideoSuccess(String json) {
		SendMessage("OnPickVideoSuccess", json);
	}

	private static void onRecordVideoError(String message) {
		SendMessage("OnRecordVideoError", message);
	}

	private static void onRecordVideoSuccess(String json) {
		SendMessage("OnRecordVideoSuccess", json);
	}

	public static void onPickFileError(String message) {
		SendMessage("OnPickFileError", message);
	}

	public static void onPickFileSuccess(String json) {
		SendMessage("OnPickFileSuccess", json);
	}

	public static void onPrintSuccess()
	{
		SendMessage("OnPrintSuccess", "");
	}

	public static void onNotificationReceived(int notificationId)
	{
		SendMessage("OnNotificationReceived", String.valueOf(notificationId));
	}
}
