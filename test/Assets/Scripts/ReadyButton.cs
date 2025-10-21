using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using System.Linq;
using static SDController;

public class ReadyButton : MonoBehaviour
{
    [SerializeField] private Drag3DObject dragmanager;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private MonsterCardGenerator cardGenerator;
    [SerializeField] private CPUController CPU;

    private TextToImage _t2I = new TextToImage();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public async Task OnClick() 
    {
        var monsterCardObj = await cardGenerator.CreateMonsterCardAsync(
            dragmanager.CardsInFieldSlot,
            spawnPoint,
            isPlayer: true
            );

        if(monsterCardObj != null) dragmanager.CardsInFieldSlot.Add(monsterCardObj);
        CPU.ExecuteCPUActionAsync().Forget(); // 仮置き

    }

}
