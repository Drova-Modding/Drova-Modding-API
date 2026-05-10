import sys
### This file is to create a mapping file and with this mapping file creating a TTS yaml file

# Check if a filename was provided as an argument
if len(sys.argv) < 2:
    print(f"Usage: python {sys.argv[0]} dialogueFile.txt")
    sys.exit(1)

input_file = sys.argv[1]

name_to_id = {}
current_id = 1

with open(input_file, 'r', encoding='utf-8') as f:
    for line in f:
        line = line.strip()
        if not line:
            continue
        parts = line.split('|')
        name = parts[0]
        if name not in name_to_id:
            name_to_id[name] = current_id
            current_id += 1

mapping = {}
output_file = "mapping.txt"
with open(output_file, 'w', encoding='utf-8') as out:
    for name, id_ in name_to_id.items():
        out.write(f"{name}|{id_}\n")
        mapping[id_] = name

# id: name
yaml_file = "mapping.yaml"
with open(yaml_file, "w", encoding="utf-8") as out:
    for id_, name in sorted(mapping.items(), key=lambda x: int(x[0])):
        out.write(f"{id_}: \n")