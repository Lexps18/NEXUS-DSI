using System;
using UnityEngine;

[Serializable]
public class HeroData
{
    public string id;
    public string nombre;
    public string clase;
    public int nivel;
    public int vida;
    public int ataque;
    public int defensa;
    public string imagenPath;

    public int Poder => vida + ataque + defensa;

    public string Iniciales()
    {
        if (string.IsNullOrEmpty(nombre)) return "??";
        string[] partes = nombre.Trim().Split(' ');
        if (partes.Length >= 2)
            return ("" + partes[0][0] + partes[1][0]).ToUpper();
        return nombre.Substring(0, Mathf.Min(2, nombre.Length)).ToUpper();
    }

    public string BadgeClass()
    {
        return clase switch
        {
            "Mago" => "badge-red",
            "Arquero" => "badge-green",
            "Paladin" => "badge-blue",
            _ => "badge-gold"
        };
    }
}

[Serializable]
public class HeroCollection
{
    public System.Collections.Generic.List<HeroData> heroes = new();
}