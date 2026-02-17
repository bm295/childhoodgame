const express = require('express');
const path = require('path');
const { Pool } = require('pg');

const app = express();
const port = process.env.PORT || 5000;

const pool = new Pool({
  user: process.env.PGUSER || 'bcfodeyhmhsrcn',
  host: process.env.PGHOST || 'ec2-54-197-230-161.compute-1.amazonaws.com',
  database: process.env.PGDATABASE || 'd9lmukb58d9f3g',
  password: process.env.PGPASSWORD || '39021d98ad2a8aeaffda294b550bd73f560e423afba332b26e7c925dcf846a21',
  port: Number(process.env.PGPORT || 5432),
  ssl: { rejectUnauthorized: false }
});

const fallbackAssets = [
  {
    Title: 'Starter Farm',
    Description: 'Grow your first crops and learn the basics.',
    ImgURL: 'https://images.unsplash.com/photo-1500937386664-56d1dfef3854?auto=format&fit=crop&w=600&q=80',
    Level: 1
  },
  {
    Title: 'Tool Shed',
    Description: 'Upgrade your tools to gather resources faster.',
    ImgURL: 'https://images.unsplash.com/photo-1501004318641-b39e6451bec6?auto=format&fit=crop&w=600&q=80',
    Level: 4
  },
  {
    Title: 'Village Market',
    Description: 'Trade your harvest and discover rare items.',
    ImgURL: 'https://images.unsplash.com/photo-1488459716781-31db52582fe9?auto=format&fit=crop&w=600&q=80',
    Level: 8
  }
];

app.use(express.json());

app.get('/api/assets', async (_req, res) => {
  try {
    const result = await pool.query('SELECT "Title", "Description", "ImgURL", "Level" FROM "Assets";');
    res.json(result.rows);
  } catch (error) {
    console.error('Failed to load assets from PostgreSQL, serving fallback data.', error.message);
    res.json(fallbackAssets);
  }
});

const distPath = path.join(__dirname, 'dist', 'childhoodgame', 'browser');
app.use(express.static(distPath));

app.get('*', (_req, res) => {
  res.sendFile(path.join(distPath, 'index.html'));
});

app.listen(port, () => {
  console.log(`Server listening on http://localhost:${port}`);
});
