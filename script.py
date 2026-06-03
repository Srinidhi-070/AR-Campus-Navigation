import json
data=json.load(open('ARBackend/nodes.json', encoding='utf-8'))
stairs6 = [n for n in data['nodes'] if n['floor'] == 6 and n.get('type') in ('staircase', 'lift')]
stairs7 = [n for n in data['nodes'] if n['floor'] == 7 and n.get('type') in ('staircase', 'lift')]
print(f'Floor 6 stairs/lifts: {len(stairs6)}')
for n in stairs6[:5]: print(f"{n['id']} at {n['x']}, {n['z']}")
print(f'Floor 7 stairs/lifts: {len(stairs7)}')
for n in stairs7[:5]: print(f"{n['id']} at {n['x']}, {n['z']}")
