import json
with open('ARBackend/nodes.json', 'r', encoding='utf-8') as f:
    data = json.load(f)
nodes = {n['id']: n for n in data['nodes']}
count = 0
for node_id, node in nodes.items():
    if '6TH_FLOOR' in node_id and node.get('type') in ['staircase', 'lift']:
        for n_id in node.get('neighbors', []):
            if '7TH_FLOOR' in n_id:
                print(node_id + ' -> ' + n_id)
                count += 1
print('Total links from 6 to 7:', count)

