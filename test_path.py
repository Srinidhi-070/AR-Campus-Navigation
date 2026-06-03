import sys
sys.path.append('ARBackend')
from services.graph_service import GraphService
g = GraphService('ARBackend/nodes.json')
try:
    path = g.get_path('AB_802_AD_FACULTY_LOUNGE', 'AB_AUDITORIUM_ENTRANCE_1')
    print('Path found!')
    print(f'Length: {len(path.path)} nodes')
    print('Directions:', path.directions)
except Exception as e:
    print(f'Error: {e}')
