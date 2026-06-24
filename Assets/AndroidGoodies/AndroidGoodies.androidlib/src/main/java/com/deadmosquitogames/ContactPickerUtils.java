package com.deadmosquitogames;

import android.app.Activity;
import android.content.Intent;
import android.util.Log;

import com.deadmosquitogames.multipicker.api.CacheLocation;
import com.deadmosquitogames.multipicker.api.ContactPicker;
import com.deadmosquitogames.multipicker.api.callbacks.ContactPickerCallback;
import com.deadmosquitogames.multipicker.api.entity.ChosenContact;
import com.deadmosquitogames.util.Constants;
import com.deadmosquitogames.util.JsonUtil;
import com.deadmosquitogames.util.UnityUtil;

class ContactPickerUtils {

	static void pickContact(Activity activity) {
		ContactPicker picker = new ContactPicker(activity);
		picker.pickContact();
	}

	static void handleContactReceived(int resultCode, Intent intent, final Activity activity) {
		if (resultCode != Activity.RESULT_OK) {
			UnityUtil.onContactPickError("Picking contact was cancelled");
			return;
		}

		ContactPicker picker = new ContactPicker(activity);
		picker.setCacheLocation(CacheLocation.INTERNAL_APP_DIR);

		picker.setContactPickerCallback(new ContactPickerCallback() {
			@Override
			public void onContactChosen(ChosenContact contact) {
				String json = JsonUtil.serializeContact(contact);
				Log.d(Constants.LOG_TAG, "Picked contact:" + json);
				UnityUtil.onContactPickSuccess(json);
			}

			@Override
			public void onError(String message) {
				UnityUtil.onContactPickError(message);
			}
		});
		picker.submit(intent);
	}
}
