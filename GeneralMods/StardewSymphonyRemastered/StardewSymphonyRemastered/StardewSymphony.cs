﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Xna.Framework.Audio;
using StardewModdingAPI;
using StardewValley;
using StardewSymphonyRemastered.Framework;
using System.IO;

namespace StardewSymphonyRemastered
{

    /// <summary>
    /// BIG WIP. Don't use this at all because it does nothing right now.
    /// TODO:
    /// 1.Make Xwb packs work
    /// 1.5. Make way to load in music packs.
    /// 2.Make stream files work
    /// 2.5. Make Music Manager
    /// 3.Make interface.
    /// 4.Make sure stuff doesn't blow up.
    /// 5.Release
    /// 6.Make videos documenting how to make this mod work.
    /// 7.Make way to generate new music packs.
    /// </summary>
    public class StardewSymphony : Mod
    {
        public static WaveBank DefaultWaveBank;
        public static SoundBank DefaultSoundBank;


        public static IModHelper ModHelper;
        public static IMonitor ModMonitor;

        public static MusicManager musicManager;

        private string MusicPath;
        public static string WavMusicDirectory;
        public static string XACTMusicDirectory;
        public static string TemplateMusicDirectory;

        public bool musicPacksInitialized;

        public override void Entry(IModHelper helper)
        {
            DefaultSoundBank = Game1.soundBank;
            DefaultWaveBank = Game1.waveBank;
            ModHelper = helper;
            ModMonitor = Monitor;

            StardewModdingAPI.Events.SaveEvents.AfterLoad += SaveEvents_AfterLoad;
            StardewModdingAPI.Events.LocationEvents.CurrentLocationChanged += LocationEvents_CurrentLocationChanged;
            StardewModdingAPI.Events.GameEvents.UpdateTick += GameEvents_UpdateTick;
            musicManager = new MusicManager();

            MusicPath = Path.Combine(ModHelper.DirectoryPath, "Content", "Music");
            WavMusicDirectory = Path.Combine(MusicPath, "Wav");
            XACTMusicDirectory = Path.Combine(MusicPath, "XACT");
            TemplateMusicDirectory = Path.Combine(MusicPath, "Templates");

            this.createDirectories();
            this.createBlankXACTTemplate();

            musicPacksInitialized = false;
        }

        private void GameEvents_UpdateTick(object sender, EventArgs e)
        {
            if (Game1.activeClickableMenu.GetType()!=typeof(StardewValley.Menus.TitleMenu)&& Game1.audioEngine.isNull()) return;
            if (musicPacksInitialized == false)
            {
                initializeMusicPacks();
                musicPacksInitialized = true;
            }
        }

        public void initializeMusicPacks()
        {
            //load in all packs here.
            loadXACTMusicPacks();
        }

        public void createDirectories()
        {
            if (!Directory.Exists(MusicPath)) Directory.CreateDirectory(MusicPath);
            if (!Directory.Exists(WavMusicDirectory)) Directory.CreateDirectory(WavMusicDirectory);
            if (!Directory.Exists(XACTMusicDirectory)) Directory.CreateDirectory(XACTMusicDirectory);
            if (!Directory.Exists(TemplateMusicDirectory)) Directory.CreateDirectory(TemplateMusicDirectory);
        }
        public void createBlankXACTTemplate()
        {
            string path= Path.Combine(TemplateMusicDirectory, "XACT");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            if(!File.Exists(Path.Combine(path, "MusicPackInformation.json"))){
                MusicPackMetaData blankMetaData = new MusicPackMetaData("Omegas's Music Data Example","Omegasis","Just a simple example of how metadata is formated for music packs. Feel free to copy and edit this one!","1.0.0 CoolExample");
                blankMetaData.writeToJson(Path.Combine(path, "MusicPackInformation.json"));
            }
            if (!File.Exists(Path.Combine(path, "readme.txt")))
            {
                string info = "Place the Wave Bank.xwb file and Sound Bank.xsb file you created in XACT in a similar directory in Content/Music/XACT/SoundPackName with a new meta data to load it!";
                File.WriteAllText(Path.Combine(path, "readme.txt"),info);
            }
        }


        public static void loadXACTMusicPacks()
        {
            string[] listOfDirectories= Directory.GetDirectories(XACTMusicDirectory);
            foreach(string folder in listOfDirectories)
            {
                //This chunk essentially allows people to name .xwb and .xsb files whatever they want.
                string[] xwb=Directory.GetFiles(folder, "*.xwb");
                string[] xsb = Directory.GetFiles(folder, "*.xsb");

                string[] debug = Directory.GetFiles(folder);
                if (xwb.Length == 0)
                {
                    ModMonitor.Log("Error loading in attempting to load music pack from: " + folder + ". There is no wave bank music file: .xwb located in this directory. AKA there is no valid music here.", LogLevel.Error);
                    return;
                }
                if (xwb.Length >= 2)
                {
                    ModMonitor.Log("Error loading in attempting to load music pack from: " + folder + ". There are too many wave bank music files or .xwbs located in this directory. Please ensure that there is only one music pack in this folder. You can make another music pack but putting a wave bank file in a different folder.", LogLevel.Error);
                    return;
                }

                if (xsb.Length == 0)
                {
                    ModMonitor.Log("Error loading in attempting to load music pack from: " + folder + ". There is no sound bank music file: .xsb located in this directory. AKA there is no valid music here.", LogLevel.Error);
                    return;
                }
                if (xsb.Length >= 2)
                {
                    ModMonitor.Log("Error loading in attempting to load music pack from: " + folder + ". There are too many sound bank music files or .xsbs located in this directory. Please ensure that there is only one sound reference file in this folder. You can make another music pack but putting a sound file in a different folder.", LogLevel.Error);
                    return;
                }

                string waveBank = xwb[0];
                string soundBank = xsb[0];
                string metaData = Path.Combine(folder, "MusicPackInformation.json");

                if (!File.Exists(metaData))
                {
                    ModMonitor.Log("WARNING! Loading in a music pack from: " + folder + ". There is no MusicPackInformation.json associated with this music pack meaning that while songs can be played from this pack, no information about it will be displayed.", LogLevel.Error);
                }
                StardewSymphonyRemastered.Framework.XACTMusicPack musicPack = new XACTMusicPack(folder, waveBank,soundBank);
                musicManager.addMusicPack(musicPack,true,true);
            }
        }


        /// <summary>
        /// Raised when the player changes locations. This should determine the next song to play.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LocationEvents_CurrentLocationChanged(object sender, StardewModdingAPI.Events.EventArgsCurrentLocationChanged e)
        {
            musicManager.selectMusic(SongSpecifics.getCurrentConditionalString());
        }

        /// <summary>
        /// Events to occur after the game has loaded in.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveEvents_AfterLoad(object sender, EventArgs e)
        {
            StardewSymphonyRemastered.Framework.SongSpecifics.addLocations();
            StardewSymphonyRemastered.Framework.SongSpecifics.addFestivals();
            StardewSymphonyRemastered.Framework.SongSpecifics.addEvents();

           

           
        }


        /// <summary>
        /// Reset the music files for the game.
        /// </summary>
        public static void Reset()
        {
            Game1.waveBank = DefaultWaveBank;
            Game1.soundBank = DefaultSoundBank;
        }


    }
}
