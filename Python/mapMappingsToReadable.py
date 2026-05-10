# Define keywords or patterns for narrator/non-human text
narrator_keywords = ["WRONG", "EntityInfo_", "MineCookingFire", "Critter_", "Aldo_Corpse", "Sheep", "Canary", "BlueVomit", "Piglet_", "LeynasTable", "BeeHive", "Broken", "Table_", "WellLadder", "item_", "INSTIGATOR", "Actor", "SheepTrace", "MusicMachine", "Mysterious", "Obsidian", "Schlund", "Crime", "GrowthEnhancer", "Skeleton", "Fossil", "Pedestral", "RedTower", "HumanFight", "Poo", "Lantern", "CentralStation", "Cobweb", "Generic", "Judge", "Child", "Wall", "Podest", "Machine", "Trace", "Plant", "Wool", "Fight", "Root", "Ladder", "Hive", "Fence", "Growth", "Enhancer", "Skeleton", "Fossil", "Pedestral", "RedTower", "HumanFight", "Poo", "Lantern", "CentralStation", "Cobweb", "Generic", "Judge", "Child", "Wall", "Podest", "Machine", "Trace", "Plant", "Wool", "Fight", "Root", "Ladder", "Hive", "Fence", "Beard"]

# Function to check if a line contains narrator/non-human text
def is_narrator(line):
    for keyword in narrator_keywords:
        if keyword in line:
            return True
    return False

# Read the input file
input_file = "mapping.txt"
with open(input_file, "r") as file:
    lines = file.readlines()

# Separate lines into narrator and NPC files
narrator_lines = []
npc_lines = []

for line in lines:
    if is_narrator(line):
        narrator_lines.append(line)
    else:
        npc_lines.append(line)

# Write narrator lines to a new file
with open("narrator.txt", "w") as file:
    file.writelines(narrator_lines)

# Write NPC lines to a new file
with open("npcs.txt", "w") as file:
    file.writelines(npc_lines)

print("Separation complete! Check narrator.txt and npcs.txt.")