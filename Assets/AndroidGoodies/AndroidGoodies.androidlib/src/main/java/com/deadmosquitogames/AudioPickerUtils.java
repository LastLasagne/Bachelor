package com.deadmosquitogames;

import android.app.Activity;
import android.content.Intent;
import androidx.annotation.NonNull;
import android.util.Log;
import com.deadmosquitogames.multipicker.api.AudioPicker;
import com.deadmosquitogames.multipicker.api.CacheLocation;
import com.deadmosquitogames.multipicker.api.callbacks.AudioPickerCallback;
import com.deadmosquitogames.multipicker.api.entity.ChosenAudio;
import com.deadmosquitogames.util.Constants;
import com.deadmosquitogames.util.JsonUtil;
import com.deadmosquitogames.util.UnityUtil;

import java.util.List;

class AudioPickerUtils {
	public static void pick(Activity context) {
		try {
			AudioPicker audioPicker = new AudioPicker(context);
//			audioPicker.allowMultiple();
			audioPicker.setAudioPickerCallback(getAudioPickerCallback());
			audioPicker.pickAudio();
		} catch (Exception e) {
			UnityUtil.onPickAudioError("Picking audio failed");
		}
	}

	public static void handleAudioReceived(int resultCode, Intent intent, Activity context) {
		if (resultCode != Activity.RESULT_OK) {
			UnityUtil.onPickAudioError("Picking audio was cancelled");
			return;
		}

		AudioPicker audioPicker = new AudioPicker(context);
		audioPicker.setCacheLocation(CacheLocation.INTERNAL_APP_DIR);
		audioPicker.setAudioPickerCallback(getAudioPickerCallback());
		audioPicker.submit(intent);
	}

	@NonNull
	private static AudioPickerCallback getAudioPickerCallback() {
		return new AudioPickerCallback() {
			@Override
			public void onAudiosChosen(List<ChosenAudio> audios) {
				if (audios.isEmpty()) {
					UnityUtil.onPickAudioError("List of audios is empty somehow");
					return;
				}

				String json = JsonUtil.serializeAudio(audios.get(0));
				Log.d(Constants.LOG_TAG, "Picked audio:" + json);
				UnityUtil.onPickAudioSuccess(json);
			}

			@Override
			public void onError(String message) {
				UnityUtil.onPickAudioError(message);
			}
		};
	}
}
