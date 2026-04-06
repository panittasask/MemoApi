# Workflow API Parameter Examples

## 1. Create Workflow
**POST /api/workflow**
```json
{
  "name": "My Workflow",
  "description": "รายละเอียด workflow"
}
```

---

## 2. Update Workflow
**PUT /api/workflow/{id}**
```json
{
  "name": "ชื่อใหม่ของ Workflow",
  "description": "รายละเอียดใหม่"
}
```

---

## 3. Delete Workflow
**DELETE /api/workflow/{id}**
ไม่มี body (ส่ง id ใน path)

---

## 4. Get Workflow List
**GET /api/workflow**
ไม่มี body (ไม่ต้องส่ง parameter)

---

## 5. Get Workflow Detail
**GET /api/workflow/{id}**
ไม่มี body (ไม่ต้องส่ง parameter)

---

## 6. Create Node
**POST /api/workflow/{workflowId}/nodes**
```json
{
  "nodeType": "Task",      // หรือ "Custom"
  "taskId": 1,              // ถ้าเป็น Task
  "customName": "ตรวจสอบ"  // ถ้าเป็น Custom
}
```

---

## 7. Update Node
**PUT /api/workflow/{workflowId}/nodes/{nodeId}**
```json
{
  "nodeType": "Task",
  "taskId": 2,
  "customName": null
}
```

---

## 8. Delete Node
**DELETE /api/workflow/{workflowId}/nodes/{nodeId}**
ไม่มี body (ส่ง id ใน path)

---

## 9. Create Edge
**POST /api/workflow/{workflowId}/edges**
```json
{
  "fromNodeId": 10,
  "toNodeId": 11
}
```

---

## 10. Delete Edge
**DELETE /api/workflow/{workflowId}/edges/{edgeId}**
ไม่มี body (ส่ง id ใน path)
