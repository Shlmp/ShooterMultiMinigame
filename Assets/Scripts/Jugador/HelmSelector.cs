using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class HelmSelector : MonoBehaviour
{
    public void SelectVisorHelmet() {Jugador.helmSelected = "VisorHelmet";}
    public void SelectJingasaHelmet() {Jugador.helmSelected = "JingasaHelmet";}
    public void SelectMedievalJHelmet() {Jugador.helmSelected = "MedievalHelmet";}
}
