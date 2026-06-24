package com.deadmosquitogames.multipicker.api.callbacks;

import com.deadmosquitogames.multipicker.api.entity.ChosenAudio;

import java.util.List;

/**
 * Created by kbibek on 2/23/16.
 */
public interface AudioPickerCallback extends PickerCallback {
    void onAudiosChosen(List<ChosenAudio> audios);
}
