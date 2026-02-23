using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CutScene : MonoBehaviour
{
    [SerializeField] private GameObject[] CutSceneList;
    [SerializeField] private float delayTime = 5f; // 컷씬 사이의 대기 시간 (1초)

    private int _curIdx = 0;
    private Coroutine _autoPlayCoroutine;

    private void Start()
    {
        foreach (var cut in CutSceneList) cut.SetActive(false);
        
        _autoPlayCoroutine = StartCoroutine(InitialStart());
    }

    private IEnumerator InitialStart()
    {
        yield return new WaitForSeconds(2f);
        PlayNext();
    }
    
    public void PlayNext()
    {
        if (_autoPlayCoroutine != null)
        {
            StopCoroutine(_autoPlayCoroutine);
        }
        
        if (_curIdx < CutSceneList.Length)
        {
            CutSceneList[_curIdx].SetActive(true);
            _curIdx++;
            
            _autoPlayCoroutine = StartCoroutine(AutoNextDelay());
        }
        else
        {
            Hide();
        }
    }

    private IEnumerator AutoNextDelay()
    {
        yield return new WaitForSeconds(delayTime);
        PlayNext();
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
