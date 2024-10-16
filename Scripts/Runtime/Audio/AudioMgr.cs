using System.Collections.Generic;
using Engine.Scripts.Runtime.Manager;
using Engine.Scripts.Runtime.Resource;
using Engine.Scripts.Runtime.Timer;
using UnityEngine;
using UnityEngine.Playables;

namespace Engine.Scripts.Runtime.Audio
{
    struct TimeInfo
    {
        public AudioSource AudioSource { get; private set; }
        public string RelPath { get; private set; }
        public float StartAt { get; private set; }
        public float FinishAt { get; private set; }

        public TimeInfo(AudioSource source, string relPath)
        {
            AudioSource = source;
            RelPath = relPath;
            StartAt = Time.time;
            FinishAt = StartAt + AudioSource.clip.length;
        }
    }
    
    public class AudioMgr : ManagerBase<AudioMgr>
    {
        private static readonly string AUDIO_NODE_PATH = "Node/Audio.prefab";
        
        private Transform _node;

        private AudioSource _musicSource;
        private string _currMusic;
        private List<TimeInfo> _soundsList = new List<TimeInfo>();

        public bool IsMuteMusic { get; private set; }
        public bool IsMuteSound { get; private set; }
        
        public void Init(bool createListener)
        {
            InitMgr();
            
            InitNode(createListener);
            
            TimerMgr.Ins.UseUpdate(OnUpdate);
        }
        
        protected override void OnReset()
        {
            IsMuteMusic = false;
            IsMuteSound = false;
            
            ClearAll();
            
            TimerMgr.Ins.RemoveUpdate(OnUpdate);
            TimerMgr.Ins.UseUpdate(OnUpdate);
        }

        protected override void OnDisposed()
        {
            TimerMgr.Ins.RemoveUpdate(OnUpdate);

            ClearAll();
            
            RemoveNode();
        }

        public void SetMusicMute(bool isMute)
        {
            IsMuteMusic = isMute;

            _musicSource.mute = isMute;
        }

        public void SetMusicVolume(float volume)
        {
            _musicSource.volume = volume;
        }

        public void SetSoundMute(bool isMute)
        {
            IsMuteSound = isMute;

            foreach (var info in _soundsList)
                info.AudioSource.mute = isMute;
        }

        /// <summary>
        /// 异步 播放音乐
        /// </summary>
        /// <param name="relPath"></param>
        /// <param name="isLoop"></param>
        /// <param name="volume"></param>
        /// <param name="pitch"></param>
        public void PlayMusicAsync(string relPath, bool isLoop = true, float volume = 1, float pitch = 1)
        {
            if (relPath == _currMusic)
                return;

            // 清除音乐
            ClearMusic();
            
            _currMusic = relPath;
            
            // 加载音乐
            ResMgr.Ins.GetAssetAsync<AudioClip>(relPath, clip =>
            {
                if (relPath != _currMusic)
                {
                    ResMgr.Ins.ReduceABRef(relPath);
                    
                    return;
                }
                
                _musicSource.clip = clip;
                _musicSource.loop = isLoop;
                _musicSource.mute = IsMuteMusic;
                _musicSource.volume = volume;
                _musicSource.pitch = pitch;
                _musicSource.Play();
            });
        }

        /// <summary>
        /// 播放音乐
        /// </summary>
        /// <param name="relPath"></param>
        /// <param name="isLoop"></param>
        /// <param name="volume"></param>
        /// <param name="pitch"></param>
        public void PlayMusic(string relPath, bool isLoop = true, float volume = 1, float pitch = 1)
        {
            if (relPath == _currMusic)
                return;
            
            // 清除音乐
            ClearMusic();

            _currMusic = relPath;

            var clip = ResMgr.Ins.GetAsset<AudioClip>(relPath);
            _musicSource.clip = clip;
            _musicSource.loop = isLoop;
            _musicSource.mute = IsMuteMusic;
            _musicSource.volume = volume;
            _musicSource.pitch = pitch;
            _musicSource.Play();
        }

        /// <summary>
        /// 异步 播放音效
        /// </summary>
        /// <param name="relPath"></param>
        /// <param name="isLoop"></param>
        /// <param name="volume"></param>
        /// <param name="pitch"></param>
        public async void PlaySoundAsync(string relPath, bool isLoop = false, float volume = 1, float pitch = 1)
        {
            // 加载节点
            var obj = await PoolMgr.Ins.GetAsync(AUDIO_NODE_PATH);
            var source = obj.GetComponent<AudioSource>();
            obj.transform.SetParent(_node);
            
            // 加载音乐
            ResMgr.Ins.GetAssetAsync<AudioClip>(relPath, clip =>
            {
                source.clip = clip;
                source.loop = isLoop;
                source.mute = IsMuteSound;
                source.volume = volume;
                source.pitch = pitch;
                source.Play();
            
                _soundsList.Add(new TimeInfo(source, relPath));
            });
        }

        /// <summary>
        /// 播放音效
        /// </summary>
        /// <param name="relPath"></param>
        /// <param name="isLoop"></param>
        /// <param name="volume"></param>
        /// <param name="pitch"></param>
        public void PlaySound(string relPath, bool isLoop = false, float volume = 1, float pitch = 1)
        {
            var obj = PoolMgr.Ins.Get(AUDIO_NODE_PATH);
            var source = obj.GetComponent<AudioSource>();
            obj.transform.SetParent(_node);

            var clip = ResMgr.Ins.GetAsset<AudioClip>(relPath);
            
            source.clip = clip;
            source.loop = isLoop;
            source.mute = IsMuteSound;
            source.volume = volume;
            source.pitch = pitch;
            source.Play();
            
            _soundsList.Add(new TimeInfo(source, relPath));
        }
        
        /// <summary>
        /// 移除所有音乐
        /// </summary>
        public void ClearAll()
        {
            ClearMusic();

            ClearSounds();
        }

        /// <summary>
        /// 清除音乐
        /// </summary>
        void ClearMusic()
        {
            if (_musicSource.clip != null)
            {
                _musicSource.clip = null;

                if (!string.IsNullOrEmpty(_currMusic))
                {
                    ResMgr.Ins.ReduceABRef(_currMusic);
                    _currMusic = "";
                }
            }
        }

        /// <summary>
        /// 清除音效
        /// </summary>
        void ClearSounds()
        {
            foreach (var info in _soundsList)
            {
                info.AudioSource.clip = null;
                PoolMgr.Ins.Set(info.AudioSource.gameObject);
                
                ResMgr.Ins.ReduceABRef(info.RelPath);
            }
            
            _soundsList.Clear();
        }

        void OnUpdate()
        {
            for (int i = _soundsList.Count - 1; i >= 0; i--)
            {
                var info = _soundsList[i];
                
                if (!info.AudioSource.loop && Time.time >= info.FinishAt)
                {
                    info.AudioSource.clip = null;
                    PoolMgr.Ins.Set(info.AudioSource.gameObject);
                    
                    ResMgr.Ins.ReduceABRef(info.RelPath);
                    
                    _soundsList.RemoveAt(i);
                }
            }
        }

        void InitNode(bool createListener)
        {
            var obj = GameObject.Find("AudioNode");
            if (obj == null)
            {
                obj = new GameObject("AudioNode");
                if (createListener)
                    obj.AddComponent<AudioListener>();
            }

            _node = obj.transform;
            
            _musicSource = PoolMgr.Ins.Get(AUDIO_NODE_PATH).GetComponent<AudioSource>();
            _musicSource.name = "MusicSource";
        }

        void RemoveNode()
        {
            if (_node != null)
            {
                Object.Destroy(_node.gameObject);
                _node = null;
            }
        }
    }
}