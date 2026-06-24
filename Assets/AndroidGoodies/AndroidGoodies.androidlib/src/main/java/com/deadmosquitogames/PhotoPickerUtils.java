package com.deadmosquitogames;

import android.app.Activity;
import android.content.Intent;
import androidx.annotation.NonNull;
import android.util.Log;

import com.deadmosquitogames.multipicker.api.CacheLocation;
import com.deadmosquitogames.multipicker.api.ImagePicker;
import com.deadmosquitogames.multipicker.api.callbacks.ImagePickerCallback;
import com.deadmosquitogames.multipicker.api.entity.ChosenImage;
import com.deadmosquitogames.util.Constants;
import com.deadmosquitogames.util.JsonUtil;
import com.deadmosquitogames.util.SharedPrefsHelper;
import com.deadmosquitogames.util.UnityUtil;

import java.util.List;

class PhotoPickerUtils {

	static void pick(Intent intent, Activity context) {
		try {
			SharedPrefsHelper.persistImagePickerSettings(intent, context);

			ImagePicker picker = new ImagePicker(context);
			if (intent.getBooleanExtra(SharedPrefsHelper.EXTRAS_ALLOW_MULTIPLE, false)) {
				picker.allowMultiple();
			}
			picker.setCacheLocation(CacheLocation.INTERNAL_APP_DIR);
			picker.setImagePickerCallback(getSingleImagePickerCallback()); // callback here does not matter, only matters when calling submit()
			picker.pickImage();
		} catch (Exception e) {
			UnityUtil.onPickGalleryPhotoError("Failed to pick image image");
		}
	}

	static void handlePhotoReceived(int resultCode, Intent data, AndroidGoodiesActivity activity) {
		if (resultCode != Activity.RESULT_OK) {
			UnityUtil.onPickGalleryPhotoError("Picking image was cancelled");
			return;
		}

		ImagePicker picker = new ImagePicker(activity);
		SharedPrefsHelper.configureImagePicker(activity, picker);
		picker.setCacheLocation(CacheLocation.INTERNAL_APP_DIR);
		picker.setImagePickerCallback(SharedPrefsHelper.allowMultiple(activity) ? getMultipleImagePickerCallback() : getSingleImagePickerCallback());
		picker.submit(data);
	}

	@NonNull
	private static ImagePickerCallback getSingleImagePickerCallback() {
		return new ImagePickerCallback() {
			@Override
			public void onImagesChosen(List<ChosenImage> images) {
				if (images.isEmpty()) {
					UnityUtil.onPickGalleryPhotoError("The list of picked/photos images is somehow empty");
					return;
				}

				onSingleImagePicked(images);
			}

			@Override
			public void onError(String message) {
				UnityUtil.onPickGalleryPhotoError(message);
			}
		};
	}

	@NonNull
	private static ImagePickerCallback getMultipleImagePickerCallback() {
		return new ImagePickerCallback() {
			@Override
			public void onImagesChosen(List<ChosenImage> images) {
				if (images.isEmpty()) {
					UnityUtil.onPickGalleryPhotoError("The list of picked/photos images is somehow empty");
					return;
				}

				for (ChosenImage image : images) {
					if (isInvalidImage(image)) {
						UnityUtil.onPickGalleryPhotoError("One of the images is invalid");
						return;
					}
				}

				onMultipleImagePicked(images);
			}

			@Override
			public void onError(String message) {
				UnityUtil.onPickGalleryPhotoError(message);
			}
		};
	}

	private static void onMultipleImagePicked(List<ChosenImage> images) {
		String json = JsonUtil.serializeImages(images);
		Log.d(Constants.LOG_TAG, "Picked multiple images:" + json);
		UnityUtil.onPickMultipleGalleryImagesSuccess(json);
	}

	private static void onSingleImagePicked(final List<ChosenImage> images) {
		ChosenImage img = images.get(0);

		if (isInvalidImage(img)) {
			UnityUtil.onPickGalleryPhotoError("Invalid image");
			return;
		}

		String json = JsonUtil.serializeImage(img);
		Log.d(Constants.LOG_TAG, "Picked image:" + json);
		UnityUtil.onPickGalleryImageSuccess(json);
	}

	private static boolean isInvalidImage(ChosenImage img) {
		return img.getWidth() == 0 || img.getHeight() == 0;
	}
}