package com.deadmosquitogames;

import android.app.Activity;
import android.content.Intent;

import com.deadmosquitogames.multipicker.api.CacheLocation;
import com.deadmosquitogames.multipicker.api.FilePicker;
import com.deadmosquitogames.multipicker.api.callbacks.FilePickerCallback;
import com.deadmosquitogames.multipicker.api.entity.ChosenFile;
import com.deadmosquitogames.util.JsonUtil;
import com.deadmosquitogames.util.UnityUtil;

import java.util.List;

class FilePickerUtils {

	private static final String EXTRAS_MIME_TYPE = "EXTRAS_MIME_TYPE";

	public static void pickFromDevice(Intent data, Activity context) {
		try {
			String mimeType = data.getStringExtra(EXTRAS_MIME_TYPE);

			FilePicker filePicker = new FilePicker(context);
			filePicker.setFilePickerCallback(getFilePickerCallback());
			filePicker.setMimeType(mimeType);
			filePicker.pickFile();
		} catch (Exception e) {
			UnityUtil.onPickFileError("Picking file failed");
		}
	}

	private static FilePickerCallback getFilePickerCallback() {
		return new FilePickerCallback() {
			@Override
			public void onFilesChosen(List<ChosenFile> files) {
				ChosenFile file = files.get(0);
				String json = JsonUtil.serializeFile(file);
				UnityUtil.onPickFileSuccess(json);
			}

			@Override
			public void onError(String message) {
				UnityUtil.onPickFileError(message);
			}
		};
	}

	public static void handleFileReceived(int resultCode, Intent intent, Activity context) {
		if (resultCode != Activity.RESULT_OK) {
			UnityUtil.onPickFileError("Picking file was cancelled");
			return;
		}

		FilePicker picker = new FilePicker(context);
		picker.setFilePickerCallback(getFilePickerCallback());
		picker.setCacheLocation(CacheLocation.INTERNAL_APP_DIR);
		picker.submit(intent);
	}
}
