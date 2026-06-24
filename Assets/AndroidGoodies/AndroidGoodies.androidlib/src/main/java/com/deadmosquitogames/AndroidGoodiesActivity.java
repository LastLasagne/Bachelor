package com.deadmosquitogames;

import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;
import androidx.annotation.NonNull;
import android.util.Log;
import com.deadmosquitogames.multipicker.api.Picker;
import com.deadmosquitogames.util.Constants;

public class AndroidGoodiesActivity extends Activity {

	private static final String EXTRAS_PICKER_TYPE = "EXTRAS_PICKER_TYPE";

	private static final int REQ_CODE_INVALID = -1;

	private static final int REQ_CODE_PERMISSIONS = 444;

	private static Status currentStatus = Status.AVAILABLE;

	@Override
	protected void onCreate(Bundle savedInstanceState) {
		super.onCreate(savedInstanceState);

		int pickerType = getIntent().getIntExtra(EXTRAS_PICKER_TYPE, REQ_CODE_INVALID);
		if (pickerType == REQ_CODE_INVALID) {
			Log.d(Constants.LOG_TAG, "Invalid picker code!");
		}

		if (currentStatus == Status.IN_PROGRESS) {
			return;
		}
		currentStatus = Status.IN_PROGRESS;

		switch (pickerType) {
			case Picker.PICK_CONTACT:
				ContactPickerUtils.pickContact(this);
				break;
			case Picker.PICK_IMAGE_DEVICE:
				PhotoPickerUtils.pick(getIntent(), this);
				break;
			case Picker.PICK_IMAGE_CAMERA:
				TakePhotoUtils.pick(getIntent(), this);
				break;
			case Picker.PICK_AUDIO:
				AudioPickerUtils.pick(this);
				break;
			case Picker.PICK_VIDEO_DEVICE:
				VideoPickerUtils.pickFromDevice(getIntent(), this);
				break;
			case Picker.PICK_VIDEO_CAMERA:
				VideoPickerUtils.pickFromCamera(getIntent(), this);
				break;
			case Picker.PICK_FILE:
				FilePickerUtils.pickFromDevice(getIntent(), this);
				break;
			case REQ_CODE_PERMISSIONS:
				PermissionUtils.requestPermissions(getIntent(), this, REQ_CODE_PERMISSIONS);
				break;
		}
	}

	// endregion

	@Override
	protected void onActivityResult(int requestCode, int resultCode, Intent intent) {
		Log.d(Constants.LOG_TAG, ">>> onActivityResult: " + requestCode + " " + resultCode + " " + intent);
		currentStatus = Status.AVAILABLE;

		switch (requestCode) {
			case Picker.PICK_CONTACT:
				ContactPickerUtils.handleContactReceived(resultCode, intent, this);
				break;
			case Picker.PICK_IMAGE_DEVICE:
				PhotoPickerUtils.handlePhotoReceived(resultCode, intent, this);
				break;
			case Picker.PICK_IMAGE_CAMERA:
				TakePhotoUtils.handlePhotoReceived(resultCode, intent, this);
				break;
			case Picker.PICK_AUDIO:
				AudioPickerUtils.handleAudioReceived(resultCode, intent, this);
				break;
			case Picker.PICK_VIDEO_DEVICE:
				VideoPickerUtils.handleVideoReceivedGallery(resultCode, intent, this);
				break;
			case Picker.PICK_VIDEO_CAMERA:
				VideoPickerUtils.handleVideoReceivedCamera(resultCode, intent, this);
				break;
			case Picker.PICK_FILE:
				FilePickerUtils.handleFileReceived(resultCode, intent, this);
				break;
			default:
				finish();
				break;
		}
		// FINISH
		finish();
	}

	public void onRequestPermissionsResult(int requestCode, @NonNull String[] permissions, @NonNull int[] grantResults) {
		Log.d(Constants.LOG_TAG, ">>> onRequestPermissionsResult: " + requestCode + " " + permissions.length + " " + grantResults.length);
		currentStatus = Status.AVAILABLE;
		PermissionUtils.handleRequestPermissionsResult(this, permissions, grantResults);
		finish();
	}

	private enum Status {
		AVAILABLE,
		IN_PROGRESS
	}
}
