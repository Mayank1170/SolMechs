using UnityEngine;

[System.Serializable]
public class StatBlock
{
    public int HP;
    public int ATK;
    public int DEF;
    public int ENG;
    public int SPD;
    public int SYS;
    public int PROC;

    public StatBlock Clone()
    {
        return new StatBlock
        {
            HP = this.HP,
            ATK = this.ATK,
            DEF = this.DEF,
            ENG = this.ENG,
            SPD = this.SPD,
            SYS = this.SYS,
            PROC = this.PROC
        };
    }

    public void Add(StatBlock other)
    {
        this.HP += other.HP;
        this.ATK += other.ATK;
        this.DEF += other.DEF;
        this.ENG += other.ENG;
        this.SPD += other.SPD;
        this.SYS += other.SYS;
        this.PROC += other.PROC;
    }

    public static StatBlock operator +(StatBlock a, StatBlock b)
    {
        return new StatBlock
        {
            HP = a.HP + b.HP,
            ATK = a.ATK + b.ATK,
            DEF = a.DEF + b.DEF,
            ENG = a.ENG + b.ENG,
            SPD = a.SPD + b.SPD,
            SYS = a.SYS + b.SYS,
            PROC = a.PROC + b.PROC
        };
    }
}
