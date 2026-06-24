package com.deadmosquitogames;

import android.app.Activity;
import android.content.Intent;
import androidx.annotation.NonNull;
import android.util.Log;

import com.deadmosquitogames.multipicker.api.CacheLocation;
import com.deadmosquitogames.multipicker.api.CameraVideoPicker;
import com.deadmosquitogames.multipicker.api.Picker;
import com.deadmosquitogames.multipicker.api.VideoPicker;
import com.deadmosquitogames.multipicker.api.callbacks.VideoPickerCallback;
import com.deadmosquitogames.multipicker.api.entity.ChosenVideo;
import com.deadmosquitogames.multipicker.core.VideoPickerImpl;
import com.deadmosquitogames.util.Constants;
import com.deadmosquitogames.util.JsonUtil;
import com.deadmosquitogames.util.SharedPrefsHelper;
import com.deadmosquitogames.util.UnityUtil;

import java.util.List;

class VideoPickerUtils {

	private static final int DEVICE = Picker.PICK_VIDEO_DEVICE;
	private static final int CAMERA = Picker.PICK_VIDEO_CAMERA;

	public static void pickFromDevice(Intent data, Activity context) {
		try {
			SharedPrefsHelper.persistVideoPickerSettings(data, context, DEVICE);

			VideoPicker videoPicker = new VideoPicker(context);
			videoPicker.setCacheLocation(CacheLocation.INTERNAL_APP_DIR);
			videoPicker.setVideoPickerCallback(getVideoPickerCallback(DEVICE));
			videoPicker.pickVideo();
		} catch (Exception e) {
			UnityUtil.onVideoError("Picking video failed", DEVICE);
		}
	}

	public static void pickFromCamera(Intent data, Activity context) {
		try {
			CameraVideoPicker photoPicker = new CameraVideoPicker(context);
			photoPicker.setCacheLocation(CacheLocation.INTERNAL_APP_DIR);
			photoPicker.setVideoPickerCallback(getVideoPickerCallback(CAMERA));
			final String outputPath = photoPicker.pickVideo();

			if (outputPath == null) {
				UnityUtil.onVideoError("Taking video failed", CAMERA);
				Log.e(Constants.LOG_TAG, "Failed to take video");
				return;
			}

			data.putExtra(SharedPrefsHelper.EXTRAS_VIDEO_OUTPUT_PATH, outputPath);
			SharedPrefsHelper.persistVideoPickerSettings(data, context, CAMERA);
		} catch (Exception e) {
			UnityUtil.onVideoError("Picking video failed", CAMERA);
		}
	}

	static void handleVideoReceivedGallery(int resultCode, Intent intent, Activity context) {
		handleVideoResult(resultCode, intent, context, new VideoPicker(context));
	}

	static void handleVideoReceivedCamera(int resultCode, Intent intent, Activity context) {
		handleVideoResult(resultCode, intent, context, new CameraVideoPicker(context));
	}

	private static void handleVideoResult(int resultCode, Intent intent, Activity context, VideoPickerImpl picker) {
		int videoPickerType = SharedPrefsHelper.getVideoPickerType(context);

		if (resultCode != Activity.RESULT_OK) {
			UnityUtil.onVideoError("Picking video was cancelled", videoPickerType);
			return;
		}

		SharedPrefsHelper.configureVideoPicker(context, picker);
		picker.setCacheLocation(CacheLocation.INTERNAL_APP_DIR);
		picker.setVideoPickerCallback(getVideoPickerCallback(videoPickerType));
		picker.submit(intent);
	}

	@NonNull
	private static VideoPickerCallback getVideoPickerCallback(final int pickerType) {
		return new VideoPickerCallback() {
			@Override
			public void onVideosChosen(List<ChosenVideo> videos) {
				String json = JsonUtil.serializeVideo(videos.get(0));
				Log.d(Constants.LOG_TAG, "Picked video:" + json);
				UnityUtil.onVideoSuccess(json, pickerType);
			}

			@Override
			public void onError(String message) {
				UnityUtil.onVideoError(message, pickerType);
			}
		};
	}
}
