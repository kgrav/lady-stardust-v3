using UnityEngine;
using System;

public class GameController : MonoBehaviour {
    public static GameController gcon => FindObjectOfType<GameController>();
    public bool isPaused = false;
}