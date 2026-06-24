package com.deadmosquitogames.util;

import android.annotation.SuppressLint;
import android.app.Activity;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;

import com.deadmosquitogames.multipicker.api.VideoPicker;
import com.deadmosquitogames.multipicker.core.ImagePickerImpl;
import com.deadmosquitogames.multipicker.core.VideoPickerImpl;

public class SharedPrefsHelper {

	public static final String EXTRAS_PHOTO_OUTPUT_PATH = "EXTRAS_PHOTO_OUTPUT_PATH";
	public static final String EXTRAS_VIDEO_OUTPUT_PATH = "EXTRAS_VIDEO_OUTPUT_PATH";
	private static final String FILE_KEY = "ANDROID_GOODIES_PREFS";
	public static final String EXTRAS_ALLOW_MULTIPLE = "EXTRAS_ALLOW_MULTIPLE";
	private static final int MAX_SIZE_DEFAULT = 0;
	private static final String EXTRAS_GENERATE_THUMBNAILS = "EXTRAS_GENERATE_THUMBNAILS";
	private static final String EXTRAS_GENERATE_PREVIEW_IMAGES = "EXTRAS_GENERATE_PREVIEW_IMAGES";
	private static final String EXTRAS_MAX_SIZE = "EXTRAS_MAX_SIZE";
	private static final String VIDEO_PICKER_TYPE = "VIDEO_PICKER_TYPE";

	private SharedPrefsHelper() {
	}

	// region PHOTO_PICKER
	// Persist all settings that are sent from Unity as intent extras
	@SuppressLint("ApplySharedPref")
	public static void persistImagePickerSettings(Intent data, Activity context) {
		SharedPreferences.Editor editor = getPrefs(context).edit();
		if (data.hasExtra(EXTRAS_MAX_SIZE)) {
			int maxSize = data.getIntExtra(EXTRAS_MAX_SIZE, Constants.IMAGE_RESULT_SIZE_ORIGINAL);
			editor.putInt(EXTRAS_MAX_SIZE, maxSize);
		}
		if (data.hasExtra(EXTRAS_GENERATE_THUMBNAILS)) {
			boolean genThumbnails = data.getBooleanExtra(EXTRAS_GENERATE_THUMBNAILS, true);
			editor.putBoolean(EXTRAS_GENERATE_THUMBNAILS, genThumbnails);
		}

		boolean allowMultiple = data.getBooleanExtra(EXTRAS_ALLOW_MULTIPLE, false);
		editor.putBoolean(EXTRAS_ALLOW_MULTIPLE, allowMultiple);

		if (data.hasExtra(EXTRAS_PHOTO_OUTPUT_PATH)) {
			String photoOutputPath = data.getStringExtra(EXTRAS_PHOTO_OUTPUT_PATH);
			editor.putString(EXTRAS_PHOTO_OUTPUT_PATH, photoOutputPath);
		}
		editor.commit();
	}

	@SuppressLint("ApplySharedPref")
	public static void persistVideoPickerSettings(Intent data, Activity context, int videoPickerType) {
		SharedPreferences.Editor editor = getPrefs(context).edit();

		editor.putInt(VIDEO_PICKER_TYPE, videoPickerType);

		if (data.hasExtra(EXTRAS_GENERATE_PREVIEW_IMAGES)) {
			boolean genThumbnails = data.getBooleanExtra(EXTRAS_GENERATE_PREVIEW_IMAGES, true);
			editor.putBoolean(EXTRAS_GENERATE_PREVIEW_IMAGES, genThumbnails);
		}

		if (data.hasExtra(EXTRAS_VIDEO_OUTPUT_PATH)) {
			String photoOutputPath = data.getStringExtra(EXTRAS_VIDEO_OUTPUT_PATH);
			editor.putString(EXTRAS_VIDEO_OUTPUT_PATH, photoOutputPath);
		}
		editor.commit();
	}

	public static int getVideoPickerType(Activity context) {
		return getPrefs(context).getInt(VIDEO_PICKER_TYPE, -1);
	}

	private static int getMaxImageSize(Activity context) {
		return getPrefs(context).getInt(EXTRAS_MAX_SIZE, MAX_SIZE_DEFAULT);
	}

	private static boolean shouldGenerateThumbnails(Activity activity) {
		return getPrefs(activity).getBoolean(EXTRAS_GENERATE_THUMBNAILS, true);
	}

	public static boolean allowMultiple(Activity activity) {
		return getPrefs(activity).getBoolean(EXTRAS_ALLOW_MULTIPLE, true);
	}

	private static boolean shouldGeneratePreviewImages(Activity activity) {
		return getPrefs(activity).getBoolean(EXTRAS_GENERATE_PREVIEW_IMAGES, true);
	}

	public static String getPhotoOutputPath(Activity context) {
		return getPrefs(context).getString(EXTRAS_PHOTO_OUTPUT_PATH, null);
	}

	public static String getVideoOutputPath(Activity context) {
		return getPrefs(context).getString(EXTRAS_VIDEO_OUTPUT_PATH, null);
	}

	// endregion

	private static SharedPreferences getPrefs(Activity context) {
		return context.getSharedPreferences(FILE_KEY, Context.MODE_PRIVATE);
	}

	public static void configureImagePicker(Activity activity, ImagePickerImpl picker) {
		int maxSize = getMaxImageSize(activity);
		picker.ensureMaxSize(maxSize, maxSize);
		picker.shouldGenerateThumbnails(shouldGenerateThumbnails(activity));
		picker.reinitialize(getPhotoOutputPath(activity));
	}

	public static void configureVideoPicker(Activity activity, VideoPickerImpl picker) {
		picker.shouldGeneratePreviewImages(shouldGeneratePreviewImages(activity));
		picker.reinitialize(getVideoOutputPath(activity));
	}
}
