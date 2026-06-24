package com.deadmosquitogames.multipicker.api.callbacks;

import com.deadmosquitogames.multipicker.api.entity.ChosenImage;
import com.deadmosquitogames.multipicker.api.entity.ChosenVideo;

import java.util.List;

/**
 * Created by kbibek on 3/23/16.
 */
public interface MediaPickerCallback extends PickerCallback {
    void onMediaChosen(List<ChosenImage> images, List<ChosenVideo> videos);
}
