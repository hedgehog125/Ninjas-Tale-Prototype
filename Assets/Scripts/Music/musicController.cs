using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class musicController : MonoBehaviour {
	[SerializeField] private MusicTracks defaultMusic;
	public int spotMusicCooldown;

	[Header("Music")]
	[SerializeField] private AudioSource stealthMusic;
	[SerializeField] private AudioSource spottedMusic;
	[SerializeField] private AudioSource cutsceneMusic;


	[HideInInspector] public MusicTracks areaMusic;
	private MusicTracks lastAreaMusic;

	[HideInInspector] public enum MusicTracks {
		 Stealth,
		 Spotted,
		 Cutscene
	};

	[HideInInspector] public bool inCombat;

	private AudioSource GetAreaAudio() {
		AudioSource music = null;
		if (areaMusic == MusicTracks.Stealth) music = stealthMusic;
		else if (areaMusic == MusicTracks.Cutscene) music = cutsceneMusic;

		return music;
	}

	private void Start() {
		areaMusic = defaultMusic;
		lastAreaMusic = areaMusic == MusicTracks.Stealth? MusicTracks.Cutscene : MusicTracks.Stealth; // So it's not equal on the first frame
	}

	private void FixedUpdate() {
		AudioSource music = GetAreaAudio();
		if (areaMusic != lastAreaMusic) {
			stealthMusic.Stop();
			cutsceneMusic.Stop();
			if (! inCombat) {
				music.mute = false;
				music.Play();
			}

			lastAreaMusic = areaMusic;
		}

		if (inCombat) {
			music.mute = true;
			if (! spottedMusic.isPlaying) {
				if (areaMusic == MusicTracks.Stealth) {
					spottedMusic.time = (music.time / music.clip.length) * spottedMusic.clip.length;
				}
				else {
					spottedMusic.time = 0;
				}
				spottedMusic.Play();
			}
		}
		else {
			if (spottedMusic.isPlaying) {
				music.mute = false;
				if (areaMusic == MusicTracks.Stealth) {
					music.time = (spottedMusic.time / spottedMusic.clip.length) * music.clip.length;
				}
				if (! music.isPlaying) music.Play();
				spottedMusic.Stop();
			}
		}
		inCombat = false;
	}
}
