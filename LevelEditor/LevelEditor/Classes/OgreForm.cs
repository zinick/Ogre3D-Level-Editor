﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Mogre;
namespace LevelEditor.Classes
{
    class OgreForm : Mogre.SceneManager.Listener
    {
        Root mRoot;
        RenderWindow mWindow;
        RenderSystem mRSys;
        AnimationState mAnimationState = null;

        Camera mCamera;
        Boolean IsInitialized { get; set; }
        SceneManager mMgr { get; set; }

        List<string> _ConfigurationPaths;

        public OgreForm(List<string> ConfigurationPaths)
        {
            _ConfigurationPaths = ConfigurationPaths;
            IsInitialized = false;
        }

        public void Go(string currentEntity, List<KeyValuePair<int, string>> currentMaterial, float currentScale, bool? autoScale, double renderWindowHeight, double renderWindowWidth)
        {
            RedrawScene(currentEntity, currentEntity, currentMaterial, currentEntity + "node", currentScale, autoScale, renderWindowHeight, renderWindowWidth);
        }

        public string GetLinkedSkeletonName(string currentEntity)
        {
            if (!mMgr.HasEntity(currentEntity))
                return string.Empty;

            Mesh currentMesh = mMgr.GetEntity(currentEntity).GetMesh();
            return currentMesh.SkeletonName;
        }

        public List<KeyValuePair<string, string>> GetAnimations(string currentEntity)
        {
            List<KeyValuePair<string, string>> animations = new List<KeyValuePair<string, string>>();
            if (!mMgr.HasEntity(currentEntity))
                return animations;

            try
            {
                Skeleton currentSkeleton = mMgr.GetEntity(currentEntity).GetMesh().GetSkeleton();

                if (currentSkeleton != null)
                {
                    for (ushort i = 0; i < currentSkeleton.NumAnimations; i++)
                        animations.Add(new KeyValuePair<string, string>(currentSkeleton.GetAnimation(i).Name, currentSkeleton.GetAnimation(i).Length.ToString()));
                }
            }
            catch (Exception e)
            {
                //error while trying to get submesh details, print out error for now - todo, change later to be more user friendly
                animations.Add(new KeyValuePair<string, string>("Current Entity = " + currentEntity, e.Message));
            }

            return animations;
        }

        public List<string> GetSubMeshDefaultMaterials(string currentEntity)
        {
            List<string> defaultMaterials = new List<string>();
            if (!mMgr.HasEntity(currentEntity))
                return defaultMaterials;

            try
            {
                Mesh currentMesh = mMgr.GetEntity(currentEntity).GetMesh();
                Mesh.SubMeshIterator meshIter = currentMesh.GetSubMeshIterator();

                while (meshIter.MoveNext())
                    defaultMaterials.Add(meshIter.Current.MaterialName);
            }
            catch (Exception e)
            {
                //error while trying to get submesh details, print out error for now - todo, change later to be more user friendly
                defaultMaterials.Add(e.Message);
            }

            return defaultMaterials;
        }

        public void SetAnimation(string currentEntity, string animation, bool loopAnimation)
        {
            if (!mMgr.HasEntity(currentEntity))
                return;

            Entity ent = mMgr.GetEntity(currentEntity);

            try
            {
                //mAnimationState.HasEnded
                //SceneNode foo = mMgr.GetSceneNode(currentEntity + "node");
                //foo.ResetToInitialState();

                //  mAnimationState = null;
                if (mAnimationState != null)
                    mAnimationState.Enabled = false; //set the old animation to not be enabled

                //ent.GetAnimationState("Die").Enabled = false;
                mAnimationState = ent.GetAnimationState(animation); //if the animation file isn't found, then it throws an exception

                mAnimationState.TimePosition = 0;
                mAnimationState.Loop = loopAnimation;
                mAnimationState.Enabled = true;
            }
            catch (Exception ex)
            {
                mAnimationState = null;
            }
        }

        //Keeping this code in case I need to use it for the Level Editor
        //Instead of changing the Scale on each model to look roughly the same, I know change where the camera is located
        //Whereas I might want it the other way in the level editor because I'll care about relative sizes of the models
        //public void AutoScale(string currentEntity, double renderWindowHeight, double renderWindowWidth)
        //{
        //    if (!mMgr.HasEntity(currentEntity))
        //        return;

        //    Entity ent = mMgr.GetEntity(currentEntity);
        //    Mesh currentMesh = ent.GetMesh(); //helpful: foo.numlod, numSubMeshes, numAnimations

        //    float posYScale = (float)((renderWindowHeight / 2) / (currentMesh.Bounds.Center.y + currentMesh.Bounds.HalfSize.y));
        //    float posXScale = (float)((renderWindowWidth / 2) / (currentMesh.Bounds.Center.x + currentMesh.Bounds.HalfSize.x));
        //    float negYScale = (float)((renderWindowHeight / 2) / (currentMesh.Bounds.Center.y - currentMesh.Bounds.HalfSize.y)) * -1;
        //    float negXScale = (float)((renderWindowWidth / 2) / (currentMesh.Bounds.Center.x - currentMesh.Bounds.HalfSize.x)) * -1;

        //    //Figure out the limiting scale, use that one
        //    if (posYScale <= posXScale && posYScale <= negYScale && posYScale <= negXScale)
        //        Scale(posYScale, posYScale, posYScale, currentMesh.Name);
        //    else if (posXScale <= posYScale && posXScale <= negYScale && posXScale <= negXScale)
        //        Scale(posXScale, posXScale, posXScale, currentMesh.Name);
        //    else if (negYScale <= posXScale && negYScale <= posYScale && negYScale <= negXScale)
        //        Scale(negYScale, negYScale, negYScale, currentMesh.Name);
        //    else
        //        Scale(negXScale, negXScale, negXScale, currentMesh.Name); 
        //}

        //todo - do I need to add camera, viewport, etc... stuff to this?
        private void RedrawScene(string attachedEntityName, string meshName, List<KeyValuePair<int, string>> materialNames, string sceneNodeName, float currentScale, bool? autoScale, double renderWindowHeight, double renderWindowWidth)
        {
            mMgr.ClearScene();
            SceneNode node = null;
            mAnimationState = null;

            //I think by default it'll have the materials set as long as they are found - this is needed if we want to change the materials used...
            Entity ent = mMgr.CreateEntity(attachedEntityName, meshName);
            Mesh currentMesh = ent.GetMesh(); //helpful: foo.numlod, numSubMeshes, numAnimations

            try
            {
                uint cnt = ent.NumSubEntities;
                uint meshCounter = 0;
                Mesh.SubMeshIterator testIter = ent.GetMesh().GetSubMeshIterator();
                while (testIter.MoveNext())
                {
                    string assignedMaterialName = materialNames.FirstOrDefault(n => n.Key == (int)meshCounter).Value;
                    if (meshCounter < cnt && assignedMaterialName != null)
                        ent.GetSubEntity(meshCounter).SetMaterialName(assignedMaterialName);

                    meshCounter++;
                    // SubMesh foobar = testIter.Current;
                    // foobar.SetMaterialName(foobar.MaterialName);
                }
            }
            catch (Exception ex)
            {
                ent.SetMaterialName(materialNames[0].Value); //todo, fix  logic
            }

            //ent.SetMaterialName("Examples/DarkMaterial");
            node = mMgr.RootSceneNode.CreateChildSceneNode(sceneNodeName);
            node.AttachObject(ent);

            //todo - make this a debug toggle (or perhaps a checkbox on the UI)
            node.ShowBoundingBox = true;

            //todo - is this a good way of doing this?
            float z = 0;
            if (ent.BoundingBox.Size.z > ent.BoundingBox.Size.x && ent.BoundingBox.Size.z > ent.BoundingBox.Size.y)
                z = ent.BoundingBox.Size.z;
            else if (ent.BoundingBox.Size.y > ent.BoundingBox.Size.x && ent.BoundingBox.Size.y > ent.BoundingBox.Size.z)
                z = ent.BoundingBox.Size.y;
            else
                z = ent.BoundingBox.Size.x;

            //Since mCameraAutoAspectRatio = true, it means: mCamera.AspectRatio = (float)(renderWindowWidth / renderWindowHeight);
            //By doing this I remove the need for scaling the object - unless you're zooming in/out
            mCamera.Position = new Vector3(ent.BoundingBox.Center.x, ent.BoundingBox.Center.y, z * 2); //*2 for buffer
            mCamera.LookAt(ent.BoundingBox.Center);
            mCamera.NearClipDistance = ent.BoundingBox.Size.z / 3;

            //Create a single point light source
            Light light2 = mMgr.CreateLight("MainLight");
            light2.Position = new Vector3(0, 10, -25);
            light2.Type = Light.LightTypes.LT_POINT;
            light2.SetDiffuseColour(1.0f, 1.0f, 1.0f);
            light2.SetSpecularColour(0.1f, 0.1f, 0.1f);
        }
        public void Resize(int width, int height)
        {
            if (mRoot == null || mWindow == null) return;
            // Need to let Ogre know about the resize...
            mWindow.Resize((uint)width, (uint)height);

            // Alter the camera aspect ratio to match the viewport
            mCamera.AspectRatio = (float)((double)width / (double)height);
        }

        public void Scale(float x, float y, float z, string currentEntity)
        {
            if (string.IsNullOrWhiteSpace(currentEntity))
                return;

            SceneNode node = mMgr.GetSceneNode(currentEntity + "node");
            Vector3 vec3 = node.GetScale();
            node.SetScale(x, y, z);
        }

        public void Rotate(float x, float y, float z, string currentEntity)
        {
            if (string.IsNullOrWhiteSpace(currentEntity))
                return;

            SceneNode node = mMgr.GetSceneNode(currentEntity + "node");
            node.Rotate(new Vector3(0, -1, 0), ((float)(x * System.Math.PI) / 180), Node.TransformSpace.TS_WORLD);
            node.Rotate(new Vector3(1, 0, 0), ((float)(y * System.Math.PI) / 180), Node.TransformSpace.TS_WORLD);
            node.Rotate(new Vector3(0, 0, 1), ((float)(z * System.Math.PI) / 180), Node.TransformSpace.TS_WORLD);
            //learning: .Yaw/.Pitch/.Roll is pretty much the same as what I'm doing above
        }

        void OgreForm_Resize(object sender, EventArgs e)
        {
            mWindow.WindowMovedOrResized();
        }

        void OgreForm_Disposed(object sender, EventArgs e)
        {
            mRoot.Dispose();
            mRoot = null;
        }

        public void Tick(Object stateInfo)
        {
            if (mRoot != null && IsInitialized == true)
            {
                renderScene();
                mRoot.RenderOneFrame();
            }
        }

        public bool Tick(string diffX, string diffY, string startX, string startY, int searchCounter)
        {
            if (mRoot != null && IsInitialized == true)
            {
                renderScene();

                if (mAnimationState != null)
                {
                    mAnimationState.AddTime(((float).01)); //todo - might not render last frame
                    if (mAnimationState.HasEnded)
                        return false;
                }

                mRoot.RenderOneFrame();
            }
            return true;
        }

        void renderScene()
        {
            // set the window's viewport as the active viewport
            mRSys._setViewport(mCamera.Viewport);
            // clear colour & depth
            mRSys.ClearFrameBuffer((uint)Mogre.FrameBufferType.FBT_COLOUR | (uint)Mogre.FrameBufferType.FBT_DEPTH);

            // render scene with overlays
            mMgr._renderScene(mCamera, mCamera.Viewport, true);
            mWindow.SwapBuffers(true);
        }

        public void AddResourceLocation(string resourceLocation)
        {
            ResourceGroupManager.Singleton.AddResourceLocation(resourceLocation, "FileSystem", "General");
            Console.WriteLine(resourceLocation);
            //todo - i seriously need a debug window
        }

        public void LoadResourceLocations(List<string> configurationPaths)
        {
            /*
            Console.WriteLine("NEW LOAD:");
           var foob = ResourceGroupManager.Singleton.ListResourceLocations("General");
           var bar = ResourceGroupManager.Singleton.ListResourceNames("General", true);
           var foop = ResourceGroupManager.Singleton.ListResourceNames("General", false);

            //ResourceGroupManager.Singleton.ClearResourceGroup("General");

           foreach (string foobar in foob)
           {
               ResourceGroupManager.Singleton.RemoveResourceLocation(foobar);
           }

            var foob2 = ResourceGroupManager.Singleton.ListResourceLocations("General");
            var bar2 = ResourceGroupManager.Singleton.ListResourceNames("General", true);
            var foop2 = ResourceGroupManager.Singleton.ListResourceNames("General", false);

            //example of manual add: _FileSystemPaths.Add("../../Media/models");
            foreach (string foo in configurationPaths)
            {
                AddResourceLocation(foo);
            }
            Console.WriteLine("DONE LOAD:");
            var foob3 = ResourceGroupManager.Singleton.ListResourceLocations("General");
            var bar3 = ResourceGroupManager.Singleton.ListResourceNames("General", true);
            var foop3 = ResourceGroupManager.Singleton.ListResourceNames("General", false);
              */
        }

        public void Init(String handle)
        {
            try
            {
                // Create root object
                mRoot = new Root();

                // Define Resources
                ConfigFile cf = new ConfigFile();
                cf.Load("./resources.cfg", "\t:=", true);
                ConfigFile.SectionIterator seci = cf.GetSectionIterator();
                String secName, typeName, archName;

                while (seci.MoveNext())
                {
                    secName = seci.CurrentKey;
                    ConfigFile.SettingsMultiMap settings = seci.Current;
                    foreach (KeyValuePair<string, string> pair in settings)
                    {
                        typeName = pair.Key;
                        archName = pair.Value;
                        ResourceGroupManager.Singleton.AddResourceLocation(archName, typeName, secName);
                    }
                }

                //Load the resources from resources.cfg and selected tab (_ConfigurationPaths)
                //LoadResourceLocations(_ConfigurationPaths);

                //example of manual add: _FileSystemPaths.Add("../../Media/models");
                foreach (string foo in _ConfigurationPaths)
                {
                    AddResourceLocation(foo);
                }



                // Setup RenderSystem
                mRSys = mRoot.GetRenderSystemByName("Direct3D9 Rendering Subsystem");
                //mRSys = mRoot.GetRenderSystemByName("OpenGL Rendering Subsystem");

                // or use "OpenGL Rendering Subsystem"
                mRoot.RenderSystem = mRSys;

                mRSys.SetConfigOption("Full Screen", "No");
                mRSys.SetConfigOption("Video Mode", "800 x 600 @ 32-bit colour");

                // Create Render Window
                mRoot.Initialise(false, "Main Ogre Window");
                NameValuePairList misc = new NameValuePairList();
                misc["externalWindowHandle"] = handle;
                misc["FSAA"] = "4";
                // misc["VSync"] = "True"; //not sure how to enable vsync to remove those warnings in Ogre.log
                mWindow = mRoot.CreateRenderWindow("Main RenderWindow", 800, 600, false, misc);

                // Init resources
                MaterialManager.Singleton.SetDefaultTextureFiltering(TextureFilterOptions.TFO_ANISOTROPIC);
                TextureManager.Singleton.DefaultNumMipmaps = 5;
                ResourceGroupManager.Singleton.InitialiseAllResourceGroups();

                // Create a Simple Scene
                //SceneNode node = null;
                mMgr = mRoot.CreateSceneManager(SceneType.ST_GENERIC, "SceneManager");
                mMgr.AmbientLight = new ColourValue(0.8f, 0.8f, 0.8f);

                mCamera = mMgr.CreateCamera("Camera");
                mWindow.AddViewport(mCamera);

                mCamera.AutoAspectRatio = true;
                mCamera.Viewport.SetClearEveryFrame(false);

                //Entity ent = mMgr.CreateEntity(displayMesh, displayMesh);

                //ent.SetMaterialName(displayMaterial);
                //node = mMgr.RootSceneNode.CreateChildSceneNode(displayMesh + "node");
                //node.AttachObject(ent);

                mCamera.Position = new Vector3(0, 0, -400);
                mCamera.LookAt(0, 0, 0);

                //Create a single point light source
                Light light2 = mMgr.CreateLight("MainLight");
                light2.Position = new Vector3(0, 10, -25);
                light2.Type = Light.LightTypes.LT_POINT;
                light2.SetDiffuseColour(1.0f, 1.0f, 1.0f);
                light2.SetSpecularColour(0.1f, 0.1f, 0.1f);

                mWindow.WindowMovedOrResized();

                IsInitialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Error,OgreForm.cs]: " + ex.Message + "," + ex.StackTrace);
            }
        }

        //void CreateSimpleScene(string displayItem)
        //{
        //    // Create a Simple Scene
        //    SceneNode node = null;
        //    mMgr = mRoot.CreateSceneManager(SceneType.ST_GENERIC, "SceneManager");
        //    mMgr.AmbientLight = new ColourValue(0.8f, 0.8f, 0.8f);

        //    mCamera = mMgr.CreateCamera("Camera");
        //    mWindow.AddViewport(mCamera);


        //    mCamera.AutoAspectRatio = true;
        //    mCamera.Viewport.SetClearEveryFrame(false);

        //    //what is displayed in the treeview must match what is in resources.cfg

        //    //  mMgr.CreateEntity(
        //    Entity ent = mMgr.CreateEntity("knot", displayItem);
        //    // Entity ent = mMgr.CreateEntity("level", "knot.mesh");

        //    ent.SetMaterialName("Examples/DarkMaterial");
        //    node = mMgr.RootSceneNode.CreateChildSceneNode("knotnode");
        //    node.AttachObject(ent);

        //    mCamera.Position = new Vector3(0, 200, -400);
        //    mCamera.LookAt(ent.BoundingBox.Center);

        //    //Create a single point light source
        //    Light light2 = mMgr.CreateLight("MainLight");
        //    light2.Position = new Vector3(0, 10, -25);
        //    light2.Type = Light.LightTypes.LT_POINT;
        //    light2.SetDiffuseColour(1.0f, 1.0f, 1.0f);
        //    light2.SetSpecularColour(0.1f, 0.1f, 0.1f);

        //    mWindow.WindowMovedOrResized();
        //}
    }
}