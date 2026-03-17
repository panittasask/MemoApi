import { Router, Request, Response } from 'express';
import { v4 as uuidv4 } from 'uuid';
import { Memo } from '../models/memo';

const router = Router();

const memos: Memo[] = [];

// GET /memos - list all memos
router.get('/', (_req: Request, res: Response) => {
  res.json(memos);
});

// GET /memos/:id - get a single memo
router.get('/:id', (req: Request, res: Response) => {
  const memo = memos.find((m) => m.id === req.params.id);
  if (!memo) {
    return res.status(404).json({ message: 'Memo not found' });
  }
  return res.json(memo);
});

// POST /memos - create a new memo
router.post('/', (req: Request, res: Response) => {
  const { title, content } = req.body as { title?: string; content?: string };
  if (!title || !content) {
    return res.status(400).json({ message: 'title and content are required' });
  }
  const now = new Date().toISOString();
  const memo: Memo = {
    id: uuidv4(),
    title,
    content,
    createdAt: now,
    updatedAt: now,
  };
  memos.push(memo);
  return res.status(201).json(memo);
});

// PUT /memos/:id - update an existing memo
router.put('/:id', (req: Request, res: Response) => {
  const index = memos.findIndex((m) => m.id === req.params.id);
  if (index === -1) {
    return res.status(404).json({ message: 'Memo not found' });
  }
  const { title, content } = req.body as { title?: string; content?: string };
  const existing = memos[index];
  const updated: Memo = {
    ...existing,
    title: title !== undefined ? title : existing.title,
    content: content !== undefined ? content : existing.content,
    updatedAt: new Date().toISOString(),
  };
  memos[index] = updated;
  return res.json(updated);
});

// DELETE /memos/:id - delete a memo
router.delete('/:id', (req: Request, res: Response) => {
  const index = memos.findIndex((m) => m.id === req.params.id);
  if (index === -1) {
    return res.status(404).json({ message: 'Memo not found' });
  }
  memos.splice(index, 1);
  return res.status(204).send();
});

export default router;
