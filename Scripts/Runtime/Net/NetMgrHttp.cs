﻿using System;
using System.Collections.Generic;
using System.Text;
using Engine.Scripts.Runtime.Utils;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace Engine.Scripts.Runtime.Net
{
    public partial class NetMgr
    {
        /// <summary>
        /// Http Get 请求。
        /// </summary>
        /// <param name="url">url</param>
        /// <param name="callback">回调</param>
        /// <param name="failedCallback">失败回调</param>
        /// <param name="searchStrData">查询字符串数据</param>
        /// <param name="headerData">请求头数据</param>
        /// <returns></returns>
        public async void HttpGet(string url, Action<byte[]> callback, Action failedCallback, 
            Dictionary<string, string> searchStrData = null, 
            Dictionary<string, string> headerData = null)
        {
            url = GetUrlWithSearchStrData(url, searchStrData);
            
            var webRequest = UnityWebRequest.Get(url);

            if (headerData != null)
                foreach (var data in headerData)
                    webRequest.SetRequestHeader(data.Key, data.Value);

            webRequest.timeout = 5;

            try
            {
                await webRequest.SendWebRequest();
            }
            catch (Exception e)
            {
                _log.Error($"[string] Http 'get' request to url:'{url}' error. err:'{e.Message}'");
                failedCallback?.Invoke();
                return;
            }
            
            if (!string.IsNullOrEmpty(webRequest.error))
            {
                _log.Error($"[string] Http 'get' request to url:'{url}' failed. err:'{webRequest.error}'");
                failedCallback?.Invoke();
                return;
            }
            
            callback?.Invoke(webRequest.downloadHandler.data);
        }

        /// <summary>
        /// Http Post 请求。
        /// </summary>
        /// <param name="url">url</param>
        /// <param name="callback">回调</param>
        /// <param name="failedCallback">失败回调</param>
        /// <param name="jsonStr">json数据</param>
        /// <param name="headerData">请求头数据</param>
        /// <param name="searchStrData">查询字符串数据</param>
        /// <param name="retryCnt">重试次数</param>
        public async void HttpPostJson(string url, Action<byte[]> callback, Action failedCallback,
            string jsonStr,
            Dictionary<string, string> headerData = null, 
            Dictionary<string, string> searchStrData = null,
            int retryCnt = 0)
        {
            url = GetUrlWithSearchStrData(url, searchStrData);

            UnityWebRequest webRequest = null;

            webRequest = UnityWebRequest.Post(url, jsonStr, "application/json");
            
            if (headerData != null)
                foreach (var data in headerData)
                    webRequest.SetRequestHeader(data.Key, data.Value);

            webRequest.timeout = 5;

            bool isSuccess = false;

            for (int i = 0; i < retryCnt + 1; i++)
            {
                try
                {
                    await webRequest.SendWebRequest();
                }
                catch (Exception e)
                {
                    _log.Error($"[byte[]] Http 'post' request to url:'{url}' error. err:'{e.Message}'");
                    continue;
                }
            
                if (!string.IsNullOrEmpty(webRequest.error))
                {
                    _log.Error($"[byte[]] Http 'post' request to url:'{url}' failed. err:'{webRequest.error}'");
                    continue;
                }

                isSuccess = true;
                break;
            }

            if (isSuccess)
                callback.Invoke(webRequest.downloadHandler.data);
            else
                failedCallback.Invoke();
        }

        /// <summary>
        /// Http Post 请求。
        /// </summary>
        /// <param name="url">url</param>
        /// <param name="callback">回调</param>
        /// <param name="failedCallback">失败回调</param>
        /// <param name="formData">表单数据</param>
        /// <param name="headerData">请求头数据</param>
        /// <param name="searchStrData">查询字符串数据</param>
        /// <param name="retryCnt">重试次数</param>
        public async void HttpPostForm(string url, Action<byte[]> callback, Action failedCallback,
            Dictionary<string, string> formData = null, 
            Dictionary<string, string> headerData = null, 
            Dictionary<string, string> searchStrData = null,
            int retryCnt = 0)
        {
            url = GetUrlWithSearchStrData(url, searchStrData);

            UnityWebRequest webRequest = null;

            webRequest = UnityWebRequest.Post(url, formData);
            
            if (headerData != null)
                foreach (var data in headerData)
                    webRequest.SetRequestHeader(data.Key, data.Value);

            webRequest.timeout = 5;
            

            bool isSuccess = false;

            for (int i = 0; i < retryCnt + 1; i++)
            {
                try
                {
                    await webRequest.SendWebRequest();
                }
                catch (Exception e)
                {
                    _log.Error($"[byte[]] Http 'post' request to url:'{url}' error. err:'{e.Message}'");
                    continue;
                }
            
                if (!string.IsNullOrEmpty(webRequest.error))
                {
                    _log.Error($"[byte[]] Http 'post' request to url:'{url}' failed. err:'{webRequest.error}'");
                    continue;
                }

                isSuccess = true;
                break;
            }

            if (isSuccess)
                callback.Invoke(webRequest.downloadHandler.data);
            else
                failedCallback.Invoke();
        }

        /// <summary>
        /// Http Get 请求。
        /// </summary>
        /// <param name="url">url</param>
        /// <param name="callback">回调</param>
        /// <param name="failedCallback">失败回调</param>
        /// <param name="searchStrData">查询字符串数据</param>
        /// <param name="headerData">请求头数据</param>
        /// <returns></returns>
        public void HttpGet(string url, Action<string> callback, Action failedCallback,  
            Dictionary<string, string> searchStrData = null, 
            Dictionary<string, string> headerData = null)
        {
            HttpGet(url, bytes =>
            {
                callback.Invoke(Encoding.UTF8.GetString(bytes));
            }, failedCallback, searchStrData, headerData);
        }

        /// <summary>
        /// Http Post 请求。
        /// </summary>
        /// <param name="url">url</param>
        /// <param name="callback">回调</param>
        /// <param name="failedCallback">失败回调</param>
        /// <param name="formData">表单数据</param>
        /// <param name="headerData">请求头数据</param>
        /// <param name="searchStrData">查询字符串数据</param>
        /// <param name="retryCnt">重试次数</param>
        public void HttpPostForm(string url, Action<string> callback, Action failedCallback,
            Dictionary<string, string> formData = null, 
            Dictionary<string, string> headerData = null, 
            Dictionary<string, string> searchStrData = null,
            int retryCnt = 0)
        {
            HttpPostForm(url, bytes =>
            {
                callback.Invoke(Encoding.UTF8.GetString(bytes));
            }, failedCallback, formData, headerData, searchStrData, retryCnt);
        }

        /// <summary>
        /// Http Post 请求。
        /// </summary>
        /// <param name="url">url</param>
        /// <param name="callback">回调</param>
        /// <param name="failedCallback">失败回调</param>
        /// <param name="jsonStr">json数据</param>
        /// <param name="headerData">请求头数据</param>
        /// <param name="searchStrData">查询字符串数据</param>
        /// <param name="retryCnt">重试次数</param>
        public void HttpPostJson(string url, Action<string> callback, Action failedCallback, string jsonStr, 
            Dictionary<string, string> headerData = null, 
            Dictionary<string, string> searchStrData = null,
            int retryCnt = 0)
        {
            HttpPostJson(url, bytes =>
            {
                callback.Invoke(Encoding.UTF8.GetString(bytes));
            }, failedCallback, jsonStr, headerData, searchStrData, retryCnt);
        }

        /// <summary>
        /// 通过查询字符串数据字典，获得拼接好的url
        /// </summary>
        /// <param name="url"></param>
        /// <param name="searchStrData"></param>
        /// <returns></returns>
        public string GetUrlWithSearchStrData(string url, Dictionary<string, string> searchStrData = null)
        {
            StringBuilder sb = new StringBuilder(url);

            if (searchStrData != null)
            {
                bool isFirst = true;
                    
                foreach (var data in searchStrData)
                {
                    if (isFirst)
                    {
                        sb.Append("?");
                        
                        isFirst = false;
                    }
                    else
                        sb.Append("&");
                    
                    sb.Append(data.Key);
                    sb.Append("=");
                    sb.Append(data.Value);
                }
            }

            return sb.ToString();
        }
    }
}