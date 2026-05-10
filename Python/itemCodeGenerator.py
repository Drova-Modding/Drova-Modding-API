import re

def generate_asset_references(input_text):
    """Generates asset reference lines from input text.

    Args:
        input_text: The input text containing lines in the specified format.

    Returns:
        A list of generated asset reference lines.
    """

    lines = input_text.splitlines()

    asset_references = []
    for line in lines:
        match = re.match(r"Name\/InternalId: ([\w/.]+) Guid: (\w+) ressourceType: Drova.Items.Item$", line)
        if match:
            name, guid = match.groups()
            correctName = name.split("/").pop(-1)
            correctName = correctName.replace(".asset", "")
            asset_reference = f"public readonly static AssetReferenceT<Item> {correctName} = new(\"{guid}\");"
            asset_references.append(asset_reference)

    return asset_references

# Example usage:
with open("addressables.txt", "r") as f:
    output_lines = generate_asset_references(f.read())
    for line in output_lines:
        print(line)

input("Press enter to exit;")
