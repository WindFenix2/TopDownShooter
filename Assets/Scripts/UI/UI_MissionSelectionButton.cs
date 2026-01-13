using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_MissionSelectionButton : UI_Button
{
    private UI_MissionSelection missionUI;
    private TextMeshProUGUI myText;

    [SerializeField] private Mission myMission;

    private void OnValidate()
    {
        gameObject.name = "Button - Select Mission: " + myMission.missionName;
    }
    public override void Start()
    {
        base.Start();
        missionUI = GetComponentInParent<UI_MissionSelection>();
        myText = GetComponentInChildren<TextMeshProUGUI>();
        myText.text = myMission.missionName;
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
        missionUI.UpdateMissionDesicription(myMission.missionDescription);
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
        missionUI.UpdateMissionDesicription("Choose a mission");
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        MissionManager.instance.SetCurrentMission(myMission);
    }
}
