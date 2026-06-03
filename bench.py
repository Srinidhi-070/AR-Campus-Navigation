
import time
import sys
sys.path.append('d:/AR_Spatial_Client/ARBackend')
from main import app
from fastapi.testclient import TestClient

client = TestClient(app)
t0 = time.time()
res = client.post('/get-path', json={'start_node_id': 'WAYPOINT_6TH_FLOOR_36_56', 'destination_node_id': 'WAYPOINT_7TH_FLOOR__47_67'})
t1 = time.time()
print(f'API request took {t1-t0:.3f}s, status: {res.status_code}')

