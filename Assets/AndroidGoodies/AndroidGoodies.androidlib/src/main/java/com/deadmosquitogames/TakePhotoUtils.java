package com.deadmosquitogames;

import android.app.Activity;
import android.content.Intent;
import androidx.annotation.NonNull;
import android.util.Log;

import com.deadmosquitogames.multipicker.api.CacheLocation;
import com.deadmosquitogames.multipicker.api.CameraImagePicker;
import com.deadmosquitogames.multipicker.api.callbacks.ImagePickerCallback;
import com.deadmosquitogames.multipicker.api.entity.ChosenImage;
import com.deadmosquitogames.util.Constants;
import com.deadmosquitogames.util.JsonUtil;
import com.deadmosquitogames.util.SharedPrefsHelper;
import com.deadmosquitogames.util.UnityUtil;

import java.util.List;

// https://github.com/coomar2841/android-multipicker-library/wiki/2.-Picking-Images
class TakePhotoUtils {
	public static void pick(Intent data, Activity context) {
		try {
			CameraImagePicker imagePicker = new CameraImagePicker(context);
			imagePicker.setImagePickerCallback(getImagePickerCallback());

			String outputPath = imagePicker.pickImage();
			if (outputPath == null) {
				UnityUtil.onTakePhotoError("Taking photo failed");
				Log.e(Constants.LOG_TAG, "Failed to take photo");
				return;
			}

			data.putExtra(SharedPrefsHelper.EXTRAS_PHOTO_OUTPUT_PATH, outputPath);
			SharedPrefsHelper.persistImagePickerSettings(data, context);
		} catch (Exception e) {
			UnityUtil.onTakePhotoError("Taking photo failed");
		}
	}

	static void handlePhotoReceived(int resultCode, Intent data, Activity context) {
		if (resultCode != Activity.RESULT_OK) {
			UnityUtil.onTakePhotoError("Taking photo was cancelled");
			return;
		}

		CameraImagePicker picker = new CameraImagePicker(context);
		SharedPrefsHelper.configureImagePicker(context, picker);
		picker.setCacheLocation(CacheLocation.INTERNAL_APP_DIR);

		picker.setImagePickerCallback(getImagePickerCallback());
		picker.submit(data);
	}

	@NonNull
	private static ImagePickerCallback getImagePickerCallback() {
		return new ImagePickerCallback() {
			@Override
			public void onImagesChosen(List<ChosenImage> images) {
				ChosenImage img = images.get(0);

				if (img.getWidth() == 0 || img.getHeight() == 0) {
					UnityUtil.onTakePhotoError("Invalid image");
					return;
				}

				String json = JsonUtil.serializeImage(img);
				Log.d(Constants.LOG_TAG, "Picked image:" + json);
				UnityUtil.onTakePhotoSuccess(json);
			}

			@Override
			public void onError(String message) {
				UnityUtil.onTakePhotoError(message);
			}
		};
	}

}
