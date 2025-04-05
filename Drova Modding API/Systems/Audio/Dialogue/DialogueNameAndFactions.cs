namespace Drova_Modding_API.Systems.Audio.Dialogue
{
    internal static class DialogueNameAndFactions
    {
        public const string PLAYER = "Player";
        public const string BANDIT = "Bandit";
        public const string HAUER = "Hauer_01";
        public const string FROG = "Frog";
        public readonly static string[] BOUNDED = ["Slawa", "Oksana", "Roarke", "Birka", "Taron", "Bertine"];
        public const string BOUNDED_LEADER = "Diemo";

        public const string BRUTUS_MINE_LEADER = "MiningChef";
        public const string BRUTUS_MINE_MALI = "Mali";
        public const string BRUTUS_MINE_KOLINA = "Kolina";
        public const string BRUTUS_MINE_GUARD = "Haron";
        public readonly static string[] BRUTUS_MINE_HAUER = ["Dalin", "Bruna", "Wind", "Baltus"];
        public readonly static string[] BRUTUS_MINE = [.. BRUTUS_MINE_HAUER, "Aldrik", BRUTUS_MINE_MALI, BRUTUS_MINE_LEADER, "Lanzo", BRUTUS_MINE_KOLINA, "Hasso", BRUTUS_MINE_GUARD];

        public const string NEMENTON_LEADER = "Gerdwin";
        public const string NEMENTON_GATE_SOUTH = "Palina";
        public const string NEMENTON_GATE_NORTH = "Adela";
        public const string NEMENTON_GATE_HAIN = "Lukan";
        public const string NEMENTON_GATE_HAIN_SOUTH = "Lukan";
        public const string NEMENTON_GATE_HAIN_NORTH = "Etzel";
        public const string NEMENTON_GATE_HAIN_NORTH_2 = "Laudine";
        public const string NEMENTON_LAUDINE = "Laudine";
        public const string NEMENTON_SISTER = "Molvina";
        public readonly static string[] NEMENTON = [NEMENTON_LEADER, NEMENTON_GATE_NORTH, NEMENTON_GATE_SOUTH, NEMENTON_LAUDINE, NEMENTON_SISTER, NEMENTON_GATE_HAIN, "Henik", "Asmus", "Mombert", "Myrte", "Femke", "Lio", "Gerion", "Luten", "Olga", "Gradan", "Chunnaic", "Bonny", "Dunja", "Mumme", "Kellan", "Muc", "Helma", "Angus", "Etzel", "Fia", "Rura", "Humbert", "Karotte", "Junali", "Aiko", "Adela", "Kendrick", "Jennifer", "Swenn", "Enya", "Harvey", "Kerr", "Fester", "Kati", "Teigen", "Niala", "Colin", "Adwin", "Hailey", "Gilwar", "Farona", "Minira", "Willi", "Erwin"];

        public const string RUINCAMP_BROTHER = "Jero";
        public const string RUINCAMP_DRUID_GUARD = "Oppo";
        public const string GATE_CONOR = "Conor";
        public const string GATE_UPPER_SEAN = "Sean";
        public const string GATE_UPPER_IVERA = "Ivera";
        public const string ARENA_GATE_JURI = "Juri";
        public const string BALDO_AS_ALDO = "Aldo";
        public const string RUINCAMP_HADEWIN = "Hadewin";
        public const string RUINCAMP_FLUNKA = "Flunka";
        public const string RUINCAMP_MIRANDA = "Miranda";
        public const string RUINCAMP_PROXIMUS = "Morvin";
        public const string BADY = "Bady";
        public readonly static string[] RUINCAMP = [RUINCAMP_BROTHER, RUINCAMP_DRUID_GUARD, GATE_CONOR, "Augis", "Hajo", "Ismar", "Elnea", "Manes", "Cuna", "Berni", "Farina", ARENA_GATE_JURI, "Banner", "Farlan", "Eugen", "Bardok", "Darwin", "RuinenlagerGuard", RUINCAMP_MIRANDA, "Gra", "Tiaa", "Tadhg", "Friedel", RUINCAMP_PROXIMUS, "Ester", "Dark", "Mottek", "Kilian", "Sorcha", "Mog", "Meluna", "Lindon", "Fulk", "Jola", GATE_UPPER_IVERA, "Gesina", "Cord", "Bady", "Baldo", "Kora", "Bernold", "Laurin", "Boris", "Gera", "Liandra", "Senga", "Grit", "Jimmy", "Sagar", "Ebru", "Ada", GATE_UPPER_SEAN, RUINCAMP_HADEWIN, RUINCAMP_FLUNKA, "Aidan", "Doro", "Leyna", "Blanda", "Emre", "Fygen", "Mareno", "Frithjof", "Lore"];

        public const string STORAGE_MINE_GUARD = "Irmina";
        public readonly static string[] MINE = ["Roisin", "Lothar", "Merik", "Shona", "Gero", "Esdert", STORAGE_MINE_GUARD, "Inja", "Levin"];

        public const string GATE_DEEP_MINE_KARA = "Kara";
        public const string GATE_MINE_KATSA = "Katsa";

        public readonly static string[] DEEP_MINE = [GATE_MINE_KATSA, GATE_DEEP_MINE_KARA, "Kessia", "Andrick", "Ruslan", "Ella", "Mursel", "Laric", "Jendra", "Joris", "Almar", "Agnessa", "Finlay", "Emko"];

        public readonly static string[] MOOR_CAMP = ["Jesko", "Zoltan", "Caspara", "Afrin", "Locke", "Jean"];

        public readonly static string[] AUWALD = ["Harald", "Aleka", "Adalgis", "Geli"];

        public readonly static string[] RED_TOWER_NEMENTON = ["Henik", "Asmus", "Mombert", NEMENTON_LAUDINE];
        public readonly static string[] RED_TOWER_RUINCAMP = ["Eugen", "Fulk", "Darwin", "Cuna"];
        public readonly static string[] RED_TOWER_DRUIDS = ["Mombert", "Darwin"];
        public readonly static string[] RED_TOWER_ALL = [.. RED_TOWER_NEMENTON, .. RED_TOWER_RUINCAMP];

        public readonly static string[] TAVERN = ["Olaf", "Tristan", "Andarta", "Evi", "Ruhan", "Sine", "Melf", "Tuz", "Marten", "Unknown", "Fawini", "Lobo", "Eoin", "Margitte", "Josi"];

        public readonly static string[] GEORGS_FARM = ["Dylara", "Delani", "Raya", "Mirron", "Jurek", "Georg", "Jerzy"];

        public readonly static string[] WOODCAMP = ["Jalina", "Cengiz", "Agilo", "Bombus", "Owain", "Larea", "Merlind", "Jendrik", "Blaan", "Carima"];

        public readonly static string[] FOREST = ["Gawan", "Babsi", "Faol"];

        public readonly static string[] RUIN_EXPLORER = ["Reija", "Amella", "Ede", "Monko"];

        public readonly static string[] HIPPIE_CAMP = ["Dylan", "Pwyll"];

        public readonly static string[] NEUTRAL = ["Ida", "Elgar", "Leru"];

        public readonly static string[] RUIN_CAMP_OTHER = ["Karmi", "Albrecht", "Jarmon"];

        public readonly static string[] GREAT_TUSK_NEMENTON = ["Henik", "Gradan", "Karotte"];
        public readonly static string[] GREAT_TUSK_RUINCAMP = ["Farlan", "Eugen", "Lindon"];
        public readonly static string[] GREAT_TUSK_DRUIDS = ["Darwin", "Mombert"];

        public readonly static string[] ARENA_TRAINING_FIGHTS = ["Carima", "Blaan", "Dark", "Mog"];
        public readonly static string[] ARENA_MAIN_FIGHTS = ["Ebru", "Liandra", "Banner", "Ada"];

        public readonly static string[] NPCs = [.. FOREST, .. BOUNDED, .. BRUTUS_MINE, .. NEMENTON, .. MOOR_CAMP, .. RUINCAMP, .. AUWALD, .. MINE, .. DEEP_MINE, .. TAVERN, .. GEORGS_FARM, .. WOODCAMP, .. RUIN_EXPLORER, .. HIPPIE_CAMP, .. NEUTRAL, .. RUIN_CAMP_OTHER, HAUER];

    }
}
