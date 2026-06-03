import json
data=json.load(open('ARBackend/nodes.json', encoding='utf-8'))
node_dict = {n['id']: n for n in data['nodes']}

vertical_nodes = [n for n in data['nodes'] if n.get('type') in ('staircase', 'lift')]

linked_count = 0
for i in range(len(vertical_nodes)):
    for j in range(i+1, len(vertical_nodes)):
        n1 = vertical_nodes[i]
        n2 = vertical_nodes[j]
        # Must be exactly one floor apart, same X, same Z, same type
        if abs(n1['floor'] - n2['floor']) == 1 and n1['x'] == n2['x'] and n1['z'] == n2['z'] and n1.get('type') == n2.get('type'):
            if n2['id'] not in n1.get('neighbors', []):
                n1.setdefault('neighbors', []).append(n2['id'])
                linked_count += 1
            if n1['id'] not in n2.get('neighbors', []):
                n2.setdefault('neighbors', []).append(n1['id'])
                linked_count += 1

with open('ARBackend/nodes.json', 'w', encoding='utf-8') as f:
    json.dump(data, f, indent=4)

import shutil
shutil.copy2('ARBackend/nodes.json', 'ARSpatialClient/Assets/ProjectCore/Resources/nodes.json')
print(f'Successfully added {linked_count} cross-floor links and synced to Unity Resources.')
