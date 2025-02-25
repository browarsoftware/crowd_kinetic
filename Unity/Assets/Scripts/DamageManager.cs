using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

        public List<DamagerLevel> lstDamagerLevels;// = new List<DamagerLevel>();
        public DamagerLevel[] TestWithArray = new DamagerLevel[5];


    [System.Serializable]
    public class DamagerLevel
    {
        public float neededDamageInPercentage = 50f;
        public Sprite damageSprite;
    }
}
