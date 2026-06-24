package com.deadmosquitogames.multipicker.api.callbacks;

import com.deadmosquitogames.multipicker.api.entity.ChosenContact;

/**
 * Created by kbibek on 2/27/16.
 */
public interface ContactPickerCallback extends PickerCallback {
    void onContactChosen(ChosenContact contact);
}
