using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PrivateRoomPopup : JoinWithCodePopupController
{
    [SerializeField] private TextMeshProUGUI roomName;
    public void SetRoomName(string name) => roomName.text = name;
}
