using System.Collections.Generic;
using Engine.Scripts.Runtime.Manager;
using Engine.Scripts.Runtime.Resource;
using Engine.Scripts.Runtime.Timer;
using UnityEngine;

namespace Engine.Scripts.Runtime.Audio
{
    public class AudioMgr : ManagerBase<AudioMgr>
    {
        private static readonly string AUDIO_NODE_PATH = "Node/Audio.prefab";
        
        private Transform _node;

        private AudioSource _musicSource;
        private string _currMusic;
        private Dictionary<AudioSource, string> _soundsDic = new Dictionary<AudioSource, string>();
        private List<AudioSource> _removeList = new List<AudioSource>();

        public bool IsMuteMusic { get; private set; }
        public bool IsMuteSound { get; private set; }
        
        public void Init()
        {
            InitMgr();
            
            InitNode();
            
            TimerMgr.Ins.UseUpdate(OnUpdate);
        }
        
        public override void OnReset()
        {
            IsMuteMusic = false;
            IsMuteSound = false;
            
            ClearAll();
        }

        public override void OnDisposed()
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

        public void SetSoundMute(bool isMute)
        {
            IsMuteSound = isMute;

            foreach (var kv in _soundsDic)
                kv.Key.mute = isMute;
        }

        /// <summary>
        /// 异步 播放音乐
        /// </summary>
        /// <param name="relPath"></param>
        /// <param name="isLoop"></param>
        public void PlayMusicAsync(string relPath, bool isLoop = true)
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
                _musicSource.Play();
            });
        }

        /// <summary>
        /// 播放音乐
        /// </summary>
        /// <param name="relPath"></param>
        /// <param name="isLoop"></param>
        public void PlayMusic(string relPath, bool isLoop = true)
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
            _musicSource.Play();
        }

        /// <summary>
        /// 异步 播放音效
        /// </summary>
        /// <param name="relPath"></param>
        /// <param name="isLoop"></param>
        public async void PlaySoundAsync(string relPath, bool isLoop = false)
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
                source.Play();
            
                _soundsDic.Add(source, relPath);
            });
        }

        /// <summary>
        /// 播放音效
        /// </summary>
        /// <param name="relPath"></param>
        /// <param name="isLoop"></param>
        public void PlaySound(string relPath, bool isLoop = false)
        {
            var obj = PoolMgr.Ins.Get(AUDIO_NODE_PATH);
            var source = obj.GetComponent<AudioSource>();
            obj.transform.SetParent(_node);

            var clip = ResMgr.Ins.GetAsset<AudioClip>(relPath);
            
            source.clip = clip;
            source.loop = isLoop;
            source.mute = IsMuteSound;
            source.Play();
            
            _soundsDic.Add(source, relPath);
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
            foreach (var info in _soundsDic)
            {
                info.Key.clip = null;
                PoolMgr.Ins.Set(info.Key.gameObject);
                
                ResMgr.Ins.ReduceABRef(info.Value);
            }
            
            _removeList.Clear();
            _soundsDic.Clear();
        }

        void OnUpdate()
        {
            _removeList.Clear();
            
            foreach (var info in _soundsDic)
            {
                if (!info.Key.loop && info.Key.time >= info.Key.maxDistance)
                {
                    _removeList.Add(info.Key);
                    
                    info.Key.clip = null;
                    PoolMgr.Ins.Set(info.Key.gameObject);
                    
                    ResMgr.Ins.ReduceABRef(info.Value);
                }
            }

            for (int i = _removeList.Count - 1; i >= 0; i--)
            {
                var source = _removeList[i];
                _soundsDic.Remove(source);
            }
        }

        void InitNode()
        {
            var obj = GameObject.Find("AudioNode");
            if (obj == null)
            {
                obj = new GameObject("AudioNode");
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