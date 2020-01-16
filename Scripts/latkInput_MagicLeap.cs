using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class latkInput_MagicLeap : MonoBehaviour {

    public MagicLeap_NewController ctl0;
    public MagicLeap_NewController ctl1;
    public LightningArtist latk;
    public Renderer collisionGuideRen;

    private float collisionDelay = 0.2f;
    private float repeatDelay = 0.5f;

    private void Awake() {
        if (latk == null) latk = GetComponent<LightningArtist>();
    }

    private void Update() {
        // draw
        if ((ctl0.triggerPressed && !ctl0.menuPressed)) {// || Input.GetKeyDown(KeyCode.Space)) {
            latk.clicked = true;
        } else {
            latk.clicked = false;
        }

        if (ctl0.triggerPressed && ctl0.menuPressed) {
            latk.inputErase();
        } else if (!ctl0.triggerPressed && ctl0.menuPressed) {
            latk.inputPush();
            latk.inputColorPick();
        }

        // new frame
        if (ctl1.triggerDown && ctl0.menuPressed) {
            latk.inputNewFrameAndCopy();
            Debug.Log("Ctl: New Frame Copy");
        } else if (ctl1.triggerDown && !ctl0.menuPressed) {
            latk.inputNewFrame();
            Debug.Log("Ctl: New Frame");
        }

        // show / hide all frames
        if ((!ctl0.menuPressed && ctl1.menuDown)) {
            latk.showOnionSkin = !latk.showOnionSkin;
            if (latk.showOnionSkin) {
                latk.inputShowFrames();
            } else {
                latk.inputHideFrames();
            }
        }

        // ~ ~ ~ ~ ~ ~ ~ ~ ~

        if (ctl0.menuPressed && ctl1.menuDown) {
            latk.inputDeleteFrame();
        }

        // *** write ***
        if (ctl0.padDirDown && ctl1.padDirDown) {
            if ((ctl0.padDown && ctl1.padPressed) || (ctl0.padPressed && ctl1.padDown)) {
                if (!latk.isWritingFile) latk.armWriteFile = true;
            }
        }

        // dir pad main
        if (ctl0.padDown) {
            if (ctl0.padDirCenter) {
                if (latk.brushMode == LightningArtist.BrushMode.ADD) {
                    latk.brushMode = LightningArtist.BrushMode.SURFACE;
                } else {
                    latk.brushMode = LightningArtist.BrushMode.ADD;
                }
            } else if (ctl0.padDirUp) {
                StartCoroutine(delayedUseCollisions());
            }
        }

        if (ctl0.padPressed) {
            if (ctl0.padDirLeft) {
                latk.brushSizeInc();
            } else if (ctl0.padDirRight) {
                latk.brushSizeDec();
            }
        }

        // dir pad alt
        if (ctl1.padDown) {
            if (ctl0.menuPressed) {
                if (ctl1.padDirCenter) {
                    // TODO capture
                } else if (ctl1.padDirUp) {
                    latk.inputNewLayer();
                } else if (ctl1.padDirLeft) {
                    latk.inputNextLayer();
                } else if (ctl1.padDirRight) {
                    latk.inputPreviousLayer();
                }
            } else {
                if (ctl1.padDirCenter) {
                    latk.inputPlay();
                } else if (ctl1.padDirUp) {
                    latk.inputFirstFrame();
                } else if (ctl1.padDirRight) {
                    latk.inputFrameBack();
                    StartCoroutine(repeatFrameBack());
                } else if (ctl1.padDirLeft) {
                    latk.inputFrameForward();
                    StartCoroutine(repeatFrameForward());
                }
            }
        }
    }

    IEnumerator repeatFrameForward() {
        yield return new WaitForSeconds(repeatDelay);
        while (ctl1.padPressed && ctl1.padDirLeft) {
            latk.inputFrameForward();
            yield return new WaitForSeconds(latk.frameInterval);
        }
    }

    IEnumerator repeatFrameBack() {
        yield return new WaitForSeconds(repeatDelay);
        while (ctl1.padPressed && ctl1.padDirRight) {
            latk.inputFrameBack();
            yield return new WaitForSeconds(latk.frameInterval);
        }
    }

    IEnumerator delayedUseCollisions() {
        yield return new WaitForSeconds(collisionDelay);
        if (ctl0.padDirUp) {
            latk.useCollisions = !latk.useCollisions;
            if (collisionGuideRen != null) collisionGuideRen.enabled = latk.useCollisions;
        }
    }

}
