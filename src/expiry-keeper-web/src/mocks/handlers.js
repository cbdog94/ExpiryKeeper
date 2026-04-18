// Shared mock data — used by the Vite dev-server middleware (Node.js)
let nextId = 10

function calcDays(expireDate) {
  return Math.floor((new Date(expireDate) - new Date()) / 86400000)
}

export function createDb() {
  return [
    { id: 1, barcode: '6901028075947', name: '布洛芬缓释胶囊', category: '药品', expireDate: '2027-06-01', notes: null, notifyDaysBefore: 7 },
    { id: 2, barcode: null, name: '纯牛奶', category: '食品', expireDate: '2026-04-20', notes: '冷藏保存', notifyDaysBefore: 3 },
    { id: 3, barcode: '6920202888883', name: '海飞丝洗发水', category: '日用品', expireDate: '2025-01-01', notes: null, notifyDaysBefore: 7 },
    { id: 4, barcode: null, name: '复合维生素', category: '保健品', expireDate: '2026-05-15', notes: null, notifyDaysBefore: 14 },
    { id: 5, barcode: null, name: '感冒灵颗粒', category: '药品', expireDate: '2026-04-22', notes: null, notifyDaysBefore: 7 },
  ].map(item => ({ ...item, daysUntilExpiry: calcDays(item.expireDate) }))
}

const BARCODE_MAP = {
  '6901028075947': { name: '布洛芬缓释胶囊', category: '药品' },
  '6920202888883': { name: '海飞丝洗发水', category: '日用品' },
}

export function createHandlers(db) {
  return {
    'GET /medicines': () => ({ status: 200, body: db }),

    'POST /medicines': (body) => {
      const item = { ...body, id: nextId++, daysUntilExpiry: calcDays(body.expireDate) }
      db.push(item)
      return { status: 201, body: item }
    },

    'PUT /medicines/:id': (body, params) => {
      const id = Number(params.id)
      const idx = db.findIndex(m => m.id === id)
      if (idx === -1) return { status: 404, body: {} }
      db[idx] = { ...db[idx], ...body, id, daysUntilExpiry: calcDays(body.expireDate) }
      return { status: 200, body: db[idx] }
    },

    'DELETE /medicines/:id': (_, params) => {
      const id = Number(params.id)
      const idx = db.findIndex(m => m.id === id)
      if (idx !== -1) db.splice(idx, 1)
      return { status: 204, body: null }
    },

    'POST /medicines/lookup': (body) => {
      const result = BARCODE_MAP[body.barcode]
      if (!result) return { status: 404, body: {} }
      return { status: 200, body: result }
    },

    'POST /ocr/medicine': () => ({
      status: 200,
      body: { name: '示例商品（Mock OCR）', expireDate: '2027-01-01', category: '食品', manufacturer: 'Mock 品牌' },
    }),
  }
}
