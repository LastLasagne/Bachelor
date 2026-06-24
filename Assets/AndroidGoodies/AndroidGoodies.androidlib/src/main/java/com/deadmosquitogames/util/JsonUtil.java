package com.deadmosquitogames.util;

import android.app.Activity;

import androidx.annotation.NonNull;
import androidx.core.app.ActivityCompat;
import com.deadmosquitogames.multipicker.api.entity.ChosenAudio;
import com.deadmosquitogames.multipicker.api.entity.ChosenContact;
import com.deadmosquitogames.multipicker.api.entity.ChosenFile;
import com.deadmosquitogames.multipicker.api.entity.ChosenImage;
import com.deadmosquitogames.multipicker.api.entity.ChosenVideo;
import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import java.util.List;

public class JsonUtil {

	private static final String ORIGINAL_PATH = "originalPath";
	private static final String THUMBNAIL_PATH = "thumbnailPath";
	private static final String THUMBNAIL_SMALL_PATH = "thumbnailSmallPath";
	private static final String WIDTH = "width";
	private static final String HEIGHT = "height";
	private static final String ORIENTATION = "orientation";
	private static final String DISPLAY_NAME = "displayName";
	private static final String SIZE = "size";
	private static final String DURATION = "duration";
	private static final String MIME_TYPE = "mimeType";
	private static final String CREATED_AT = "createdAt";
	private static final String PREVIEW_IMAGE = "previewImage";
	private static final String PREVIEW_IMAGE_THUMBNAIL = "previewImageThumbnail";
	private static final String PREVIEW_IMAGE_THUMBNAIL_SMALL = "previewImageThumbnailSmall";
	private static final String RESULT = "result";
	private static final String SHOULD_SHOW_REQUEST_PERMISSION_RATIONALE = "shouldShowRequestPermissionRationale";
	private static final String PERMISSION = "permission";
	private static final String PHOTO_URI = "photoUri";
	private static final String PHONES = "phones";
	private static final String EMAILS = "emails";

	public static String serializeImages(List<ChosenImage> images) {
		JSONArray imagesJsonArr = new JSONArray();

		for (ChosenImage img : images) {
			imagesJsonArr.put(toJsonObject(img));
		}

		return imagesJsonArr.toString();
	}

	public static String serializeImage(ChosenImage img) {
		return toJsonObject(img).toString();
	}

	@NonNull
	private static JSONObject toJsonObject(ChosenImage img) {
		JSONObject json = new JSONObject();
		try {
			json.put(ORIGINAL_PATH, img.getOriginalPath());
			if (img.getThumbnailPath() != null) {
				json.put(THUMBNAIL_PATH, img.getThumbnailPath());
			}
			if (img.getThumbnailSmallPath() != null) {
				json.put(THUMBNAIL_SMALL_PATH, img.getThumbnailSmallPath());
			}
			json.put(WIDTH, img.getWidth());
			json.put(HEIGHT, img.getHeight());
			json.put(DISPLAY_NAME, img.getDisplayName());
			json.put(CREATED_AT, img.getCreatedAt().getTime());
			json.put(SIZE, img.getSize());
		} catch (JSONException e) {
			e.printStackTrace();
		}
		return json;
	}

	public static String serializeAudio(ChosenAudio audio) {
		JSONObject json = new JSONObject();
		try {
			json.put(ORIGINAL_PATH, audio.getOriginalPath());
			json.put(DISPLAY_NAME, audio.getDisplayName());
			json.put(SIZE, audio.getSize());
			json.put(MIME_TYPE, audio.getMimeType());
			if (audio.getCreatedAt() != null) {
				json.put(CREATED_AT, audio.getCreatedAt().getTime());
			}
			json.put(DURATION, audio.getDuration());
		} catch (JSONException e) {
			e.printStackTrace();
		}
		return json.toString();
	}

	public static String serializeVideo(ChosenVideo chosenVideo) {
		JSONObject json = new JSONObject();
		try {
			json.put(WIDTH, chosenVideo.getWidth());
			json.put(HEIGHT, chosenVideo.getHeight());
			json.put(PREVIEW_IMAGE, chosenVideo.getPreviewImage());
			json.put(PREVIEW_IMAGE_THUMBNAIL, chosenVideo.getPreviewThumbnail());
			json.put(PREVIEW_IMAGE_THUMBNAIL_SMALL, chosenVideo.getPreviewThumbnailSmall());
			json.put(ORIENTATION, chosenVideo.getOrientation());
			json.put(ORIGINAL_PATH, chosenVideo.getOriginalPath());
			json.put(DISPLAY_NAME, chosenVideo.getDisplayName());
			json.put(SIZE, chosenVideo.getSize());
			json.put(MIME_TYPE, chosenVideo.getMimeType());
			if (chosenVideo.getCreatedAt() != null) {
				json.put(CREATED_AT, chosenVideo.getCreatedAt().getTime());
			}
			json.put(DURATION, chosenVideo.getDuration());
		} catch (JSONException e) {
			e.printStackTrace();
		}
		return json.toString();
	}

	public static String serializeFile(ChosenFile file) {
		JSONObject json = new JSONObject();
		try {
			json.put(ORIGINAL_PATH, file.getOriginalPath());
			json.put(DISPLAY_NAME, file.getDisplayName());
			json.put(SIZE, file.getSize());
			json.put(MIME_TYPE, file.getMimeType());
			if (file.getCreatedAt() != null) {
				json.put(CREATED_AT, file.getCreatedAt().getTime());
			}
		} catch (JSONException e) {
			e.printStackTrace();
		}
		return json.toString();
	}

	public static String serializeContact(ChosenContact contact) {
		JSONObject json = new JSONObject();
		try {
			json.put(DISPLAY_NAME, contact.getDisplayName());
			json.put(PHOTO_URI, contact.getPhotoUri());
			json.put("", contact.getPhotoUri());

			JSONArray phones = new JSONArray();
			for (String phone : contact.getPhones()) {
				phones.put(phone);
			}
			json.put(PHONES, phones);

			JSONArray emails = new JSONArray();
			for (String email : contact.getEmails()) {
				emails.put(email);
			}
			json.put(EMAILS, emails);
		} catch (JSONException e) {
			e.printStackTrace();
		}
		return json.toString();
	}

	public static String serializePermissionResults(Activity activity, String[] permissions, int[] grantResults) {
		JSONObject json = new JSONObject();
		try {
			JSONArray results = new JSONArray();

			for (int i = 0; i < permissions.length; i++) {
				String permission = permissions[i];
				boolean shouldShowRequestPermissionRationale = ActivityCompat.shouldShowRequestPermissionRationale(activity, permission);
				int result = grantResults[i];

				JSONObject resultJson = new JSONObject();
				resultJson.put(PERMISSION, permission);
				resultJson.put(SHOULD_SHOW_REQUEST_PERMISSION_RATIONALE, shouldShowRequestPermissionRationale);
				resultJson.put(RESULT, result);

				results.put(resultJson);
			}

			json.put(RESULT, results);
		} catch (JSONException e) {
			e.printStackTrace();
		}
		return json.toString();
	}
}
