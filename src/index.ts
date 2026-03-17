import express from 'express';
import cors from 'cors';
import memosRouter from './routes/memos';

const app = express();
const PORT = process.env.PORT ?? 3000;

app.use(cors());
app.use(express.json());

app.use('/memos', memosRouter);

app.get('/health', (_req, res) => {
  res.json({ status: 'ok' });
});

app.listen(PORT, () => {
  console.log(`MemoApi listening on port ${PORT}`);
});

export default app;
