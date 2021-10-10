﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Veldrid;

namespace zzre.game
{
    public interface IZanzarahContainer
    {
        Framebuffer Framebuffer { get; }
        event Action OnResize;
        event Action<Key> OnKeyDown;
        event Action<Key> OnKeyUp;
    }

    public class Zanzarah : ITagContainer
    {
        private readonly ITagContainer tagContainer;
        private readonly IZanzarahContainer zanzarahContainer;

        public Game? CurrentGame { get; private set; }

        public Zanzarah(ITagContainer diContainer, IZanzarahContainer zanzarahContainer)
        {
            tagContainer = new TagContainer().FallbackTo(diContainer);
            tagContainer.AddTag(this);
            tagContainer.AddTag(zanzarahContainer);
            this.zanzarahContainer = zanzarahContainer;
            CurrentGame = new Game(this, "sc_2411", -1);
        }

        public void Update()
        {
            CurrentGame?.Update();
        }

        public void Render(CommandList finalCommandList)
        {
            CurrentGame?.Render(finalCommandList);
        }

        public void Dispose() => tagContainer.Dispose();
        public ITagContainer AddTag<TTag>(TTag tag) where TTag : class => tagContainer.AddTag(tag);
        public TTag GetTag<TTag>() where TTag : class => tagContainer.GetTag<TTag>();
        public IEnumerable<TTag> GetTags<TTag>() where TTag : class => tagContainer.GetTags<TTag>();
        public bool HasTag<TTag>() where TTag : class => tagContainer.HasTag<TTag>();
        public bool RemoveTag<TTag>() where TTag : class => tagContainer.RemoveTag<TTag>();
        public bool TryGetTag<TTag>(out TTag tag) where TTag : class => tagContainer.TryGetTag(out tag);
    }
}