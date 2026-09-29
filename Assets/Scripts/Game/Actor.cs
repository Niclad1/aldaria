using System;
using System.Collections;
using System.Collections.Generic;
using Aldaria.Rules;
using UnityEngine;

namespace Aldaria.Game
{
    /// <summary>Representação visual de um personagem ou monstro no mapa: anda célula por célula, balança parado, pisca ao levar dano.</summary>
    public sealed class Actor : MonoBehaviour
    {
        public float Speed = 3.6f;

        public Cell Cell { get; private set; }
        public bool IsMoving => moving || path.Count > 0 || animating;
        /// <summary>Célula de onde um novo caminho deve partir (a próxima, se estiver no meio de um passo).</summary>
        public Cell PlanningCell => moving ? stepCell : Cell;
        /// <summary>Onde o ator vai parar quando terminar de andar.</summary>
        public Cell Destination => path.Count > 0 ? lastQueued : PlanningCell;
        public Sprite Sprite => body.sprite;
        public Vector3 HeadPosition => transform.position + Vector3.up * (Art.ContentTop(body.sprite) * body.transform.localScale.y + 0.06f);

        SpriteRenderer body, shadow, ring;
        readonly Queue<Cell> path = new Queue<Cell>();
        Cell lastQueued, stepCell;
        Vector3 stepFrom, stepTo;
        float stepT, hop, bobPhase, flash, alpha = 1f;
        bool moving, animating;
        Color flashColor;
        Action onArrive;
        Vector3 baseScale = Vector3.one;
        Sprite idleSprite, walkSprite;
        float walkClock;

        /// <summary>Quadro alternativo usado enquanto anda (ex.: pose de corrida do PixelLab).</summary>
        public void SetWalkSprite(Sprite walk)
        {
            idleSprite = body.sprite;
            walkSprite = walk;
        }

        public static Actor Create(string name, Sprite sprite, Cell cell, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var a = go.AddComponent<Actor>();
            a.shadow = MakeRenderer(go.transform, "Sombra", Art.Shadow, 700);
            a.ring = MakeRenderer(go.transform, "Anel", Art.TeamRing, 701);
            a.ring.enabled = false;
            a.body = MakeRenderer(go.transform, "Corpo", sprite, 1000);
            a.bobPhase = UnityEngine.Random.value * 10f;
            a.Place(cell);
            return a;
        }

        static SpriteRenderer MakeRenderer(Transform parent, string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.sortingOrder = order;
            return r;
        }

        public void Place(Cell c)
        {
            path.Clear();
            moving = false;
            onArrive = null;
            Cell = c;
            transform.position = Iso.ToWorld(c);
        }

        public void Walk(List<Cell> cells, Action arrive = null)
        {
            path.Clear();
            foreach (var c in cells) path.Enqueue(c);
            if (cells.Count > 0) lastQueued = cells[cells.Count - 1];
            onArrive = arrive;
            if (!moving && path.Count == 0) Arrive();
        }

        public void Stop()
        {
            path.Clear();
            onArrive = null;
        }

        public void Face(Vector3 dir)
        {
            if (Mathf.Abs(dir.x) > 0.01f) body.flipX = dir.x < 0f;
        }

        public void FaceCell(Cell c) => Face(Iso.ToWorld(c) - transform.position);

        public void Flash(Color c)
        {
            flash = 1f;
            flashColor = c;
        }

        public void SetRing(bool on, Color color)
        {
            ring.enabled = on;
            ring.color = color;
        }

        public void ResetVisual()
        {
            alpha = 1f;
            flash = 0f;
            gameObject.SetActive(true);
            SetRing(false, Color.white);
        }

        /// <summary>Verifica se um ponto do mundo está sobre o desenho do personagem (para clicar/passar o mouse).</summary>
        public bool HitTest(Vector2 world)
        {
            if (!gameObject.activeInHierarchy || alpha < 0.5f) return false;
            var p = transform.position;
            float halfW = body.sprite.bounds.extents.x * 0.55f;
            float top = Art.ContentTop(body.sprite);
            return world.x > p.x - halfW && world.x < p.x + halfW && world.y > p.y - 0.1f && world.y < p.y + top;
        }

        /// <summary>Troca o tamanho do desenho (chefes ficam maiores).</summary>
        public void SetScale(float s) => body.transform.localScale = baseScale = new Vector3(s, s, 1f);

        public IEnumerator SlideTo(Cell c, float duration)
        {
            animating = true;
            var from = transform.position;
            var to = Iso.ToWorld(c);
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                transform.position = Vector3.Lerp(from, to, 1f - (1f - t) * (1f - t));
                yield return null;
            }
            transform.position = to;
            Cell = c;
            animating = false;
        }

        public IEnumerator JumpTo(Cell c, float duration)
        {
            animating = true;
            var from = transform.position;
            var to = Iso.ToWorld(c);
            Face(to - from);
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                transform.position = Vector3.Lerp(from, to, t);
                hop = Mathf.Sin(t * Mathf.PI) * 0.6f;
                yield return null;
            }
            hop = 0f;
            transform.position = to;
            Cell = c;
            animating = false;
        }

        public IEnumerator Lunge(Vector3 toward)
        {
            var dir = (toward - transform.position);
            dir.z = 0f;
            if (dir.sqrMagnitude < 0.0001f) yield break;
            Face(dir);
            dir = dir.normalized * 0.12f;
            var basePos = transform.position;
            for (float t = 0f; t < 1f; t += Time.deltaTime / 0.18f)
            {
                transform.position = basePos + dir * Mathf.Sin(t * Mathf.PI);
                yield return null;
            }
            transform.position = basePos;
        }

        public IEnumerator FadeOut(float duration)
        {
            for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
            {
                alpha = 1f - t;
                yield return null;
            }
            alpha = 0f;
            gameObject.SetActive(false);
        }

        void Update()
        {
            float dt = Time.deltaTime;

            if (!moving && path.Count > 0)
            {
                stepCell = path.Dequeue();
                stepFrom = transform.position;
                stepTo = Iso.ToWorld(stepCell);
                stepT = 0f;
                moving = true;
                Face(stepTo - stepFrom);
            }
            if (moving)
            {
                stepT += dt * Speed;
                float t = Mathf.Clamp01(stepT);
                transform.position = Vector3.Lerp(stepFrom, stepTo, t);
                hop = Mathf.Abs(Mathf.Sin(t * Mathf.PI)) * 0.045f;
                if (t >= 1f)
                {
                    moving = false;
                    hop = 0f;
                    Cell = stepCell;
                    if (path.Count == 0) Arrive();
                }
            }

            if (walkSprite != null)
            {
                walkClock = moving ? walkClock + dt : 0f;
                body.sprite = moving && Mathf.Repeat(walkClock, 0.36f) < 0.18f ? walkSprite : idleSprite;
            }

            bobPhase += dt;
            float squash = moving ? 0f : Mathf.Sin(bobPhase * 3.2f) * 0.022f;
            body.transform.localPosition = new Vector3(0f, hop, 0f);
            body.transform.localScale = new Vector3(baseScale.x * (1f - squash * 0.6f), baseScale.y * (1f + squash), 1f);
            shadow.transform.localScale = Vector3.one * (1f - hop * 0.6f);

            if (flash > 0f) flash -= dt * 3.5f;
            var tint = Color.Lerp(Color.white, flashColor, Mathf.Clamp01(flash));
            tint.a = alpha;
            body.color = tint;
            shadow.color = new Color(1f, 1f, 1f, alpha);

            body.sortingOrder = Iso.SortOrder(transform.position.y, 5);
        }

        void Arrive()
        {
            var cb = onArrive;
            onArrive = null;
            cb?.Invoke();
        }
    }

    /// <summary>Efeito visual rápido (brilho que cresce e some). Destrói a si mesmo.</summary>
    public sealed class Fx : MonoBehaviour
    {
        SpriteRenderer r;
        float life, maxLife, fromScale, toScale;
        Color color;

        public static Fx Spawn(Vector3 pos, Color color, float fromScale, float toScale, float life)
        {
            var go = new GameObject("Fx");
            go.transform.position = pos;
            var fx = go.AddComponent<Fx>();
            fx.r = go.AddComponent<SpriteRenderer>();
            fx.r.sprite = Art.Glow;
            fx.r.sortingOrder = 5000;
            fx.color = color;
            fx.fromScale = fromScale;
            fx.toScale = toScale;
            fx.maxLife = fx.life = life;
            fx.Update();
            return fx;
        }

        void Update()
        {
            life -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(life / maxLife);
            transform.localScale = Vector3.one * Mathf.Lerp(fromScale, toScale, t);
            r.color = new Color(color.r, color.g, color.b, color.a * (1f - t));
            if (life <= 0f) Destroy(gameObject);
        }
    }
}
