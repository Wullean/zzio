using System;
using System.IO;
using System.Numerics;
using Veldrid;
using zzio.scn;
using zzio.vfs;
using zzre.imgui;
using zzre.materials;
using zzre.rendering;

namespace zzre.tools;

public partial class SceneEditor : ListDisposable, IDocumentEditor
{
    private readonly ITagContainer diContainer;
    private readonly TwoColumnEditorTag editor;
    private readonly FlyControlsTag controls;
    private readonly FramebufferArea fbArea;
    private readonly IResourcePool resourcePool;
    private readonly OpenFileModal openFileModal;
    private readonly LocationBuffer locationBuffer;
    private readonly DebugGridRenderer gridRenderer;
    private readonly Camera camera;

    private TriggerComponent triggerComponent;

    private FOModelComponent foModelComponent;

    private event Action OnLoadScene = () => { };

    private readonly ITagContainer localDiContainer;
    private Scene? scene;

    public IResource? CurrentResource { get; private set; }
    public Window Window { get; }

    private bool ControlIsPressed = false;

    public SceneEditor(ITagContainer diContainer)
    {
        this.diContainer = diContainer;
        resourcePool = diContainer.GetTag<IResourcePool>();
        Window = diContainer.GetTag<WindowContainer>().NewWindow("Scene Editor");
        Window.AddTag(this);
        Window.InitialBounds = new Rect(float.NaN, float.NaN, 1100.0f, 600.0f);
        editor = new TwoColumnEditorTag(Window, diContainer);
        var onceAction = new OnceAction();
        Window.AddTag(onceAction);
        Window.OnContent += onceAction.Invoke;
        locationBuffer = new LocationBuffer(diContainer.GetTag<GraphicsDevice>());
        AddDisposable(locationBuffer);
        var menuBar = new MenuBarWindowTag(Window);
        menuBar.AddButton("Open", HandleMenuOpen);
        openFileModal = new OpenFileModal(diContainer)
        {
            Filter = "*.scn",
            IsFilterChangeable = false
        };
        openFileModal.OnOpenedResource += Load;

        camera = new Camera(diContainer.ExtendedWith(locationBuffer));
        AddDisposable(camera);
        controls = new FlyControlsTag(Window, camera.Location, diContainer);
        gridRenderer = new DebugGridRenderer(diContainer);
        gridRenderer.Material.LinkTransformsTo(camera);
        gridRenderer.Material.World.Ref = Matrix4x4.Identity;
        AddDisposable(gridRenderer);
        fbArea = Window.GetTag<FramebufferArea>();
        fbArea.OnResize += HandleResize;
        fbArea.OnRender += camera.Update;
        fbArea.OnRender += locationBuffer.Update;
        fbArea.OnRender += gridRenderer.Render;

        localDiContainer = diContainer
            .FallbackTo(Window)
            .ExtendedWith(this, Window, gridRenderer, locationBuffer)
            .AddTag<IAssetLoader<Texture>>(new CachedAssetLoader<Texture>(diContainer.GetTag<IAssetLoader<Texture>>()))
            .AddTag<IAssetLoader<ClumpBuffers>>(new CachedClumpAssetLoader(diContainer))
            .AddTag(camera);
        new DatasetComponent(localDiContainer);
        new WorldComponent(localDiContainer);
        new ModelComponent(localDiContainer);
        new LightComponent(localDiContainer);
        new SelectionComponent(localDiContainer);

        menuBar.AddButton("Save", SaveScene);
        menuBar.AddButton("Duplicate Selection", DuplicateCurrentSelection);
        menuBar.AddButton("Delete Selection", DeleteCurrentSelection);
        foModelComponent = new FOModelComponent(localDiContainer);
        triggerComponent = new TriggerComponent(localDiContainer);

        Window.OnKeyUp += HandleKeyUp;
        Window.OnKeyDown += HandleKeyDown;
        Window.OnContent += HandleOnContent;
    }

    private void HandleKeyDown(Key key)
    {
        if (key == Key.ControlLeft)
            ControlIsPressed = true;
    }
    private void HandleKeyUp(Key key)
    {
        if (key == Key.ControlLeft)
            ControlIsPressed = false;
        else if (ControlIsPressed)
        {
            if (key == Key.D)
                DuplicateCurrentSelection();
            else if (key == Key.S)
                SaveScene();
            else if (key == Key.X)
                DeleteCurrentSelection();
        }
    }
    private void HandleOnContent()
    {
        if (Window.IsFocused == false)
        {
            ControlIsPressed = false;
        }
    }
    private void DuplicateCurrentSelection()
    {
        triggerComponent.DuplicateCurrentTrigger();
        foModelComponent.DuplicateCurrentFoModel();

    }
    private void DeleteCurrentSelection()
    {
        triggerComponent.DeleteCurrentTrigger();
        foModelComponent.DeleteCurrentFoModel();
    }
    public void Load(string pathText)
    {
        var resource = resourcePool.FindFile(pathText);
        if (resource == null)
            throw new FileNotFoundException($"Could not find world at {pathText}");
        Load(resource);
    }

    public void Load(IResource resource) =>
        Window.GetTag<OnceAction>().Next += () => LoadSceneNow(resource);

    private void LoadSceneNow(IResource resource)
    {
        if (resource.Equals(CurrentResource))
            return;
        CurrentResource = null;
        localDiContainer.GetTag<IAssetLoader<Texture>>().Clear();
        localDiContainer.GetTag<IAssetLoader<ClumpBuffers>>().Clear();

        using var contentStream = resource.OpenContent();
        if (contentStream == null)
            throw new IOException($"Could not open scene at {resource.Path.ToPOSIXString()}");
        scene = new Scene();
        scene.Read(contentStream);

        CurrentResource = resource;
        controls.ResetView();
        fbArea.IsDirty = true;
        Window.Title = $"Scene Editor - {resource.Path.ToPOSIXString()}";
        OnLoadScene();
    }

    private void HandleResize() => camera.Aspect = fbArea.Ratio;

    private void HandleMenuOpen()
    {
        openFileModal.InitialSelectedResource = CurrentResource;
        openFileModal.Modal.Open();
    }
    private void SaveScene()
    {
        if (CurrentResource == null || scene == null)
            return;
        triggerComponent.SyncWithScene();
        foModelComponent.SyncWithScene();
        var path = Path.Combine(Environment.CurrentDirectory, "..", CurrentResource.Path.ToString());

        var stream = new FileStream(path, FileMode.Create);
        scene.Write(stream);
    }

}
