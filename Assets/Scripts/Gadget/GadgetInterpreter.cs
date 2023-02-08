﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GadgetInterpreter
{
    private GameObject parent;
    private Dialogue dialogue;

    private Dictionary<char, GameObject> layerGameObject;
    private ScriptBlock<GadgetOpcode> scriptBlock;

    private ActionInterpreter actionInterpreter;

    public GadgetInterpreter(ActionInterpreter guiActionInterpreter, GameObject parent, ScriptBlock<GadgetOpcode> scriptBlock, Dialogue dialogue = null)
    {
        this.parent = parent;
        this.scriptBlock = scriptBlock;
        this.dialogue = dialogue;

        layerGameObject = new Dictionary<char, GameObject>();
        actionInterpreter = guiActionInterpreter;
    }

    private GameObject ParentByLayer(char layer)
    {
        if (!layerGameObject.ContainsKey(layer))
        {
            GameObject emptyLayerObject = GameObject.Instantiate(GameManager.Instance.gameLayerPrefabObject, parent.transform, false);
            emptyLayerObject.name = string.Format("Group {0}", layer.ToString());

            layerGameObject.Add(layer, emptyLayerObject);
            return emptyLayerObject;
        }

        return layerGameObject[layer];
    }

    private string GetSortingLayer(char layer)
    {
        return string.Format("GameLayer{0}", Char.ToUpper(layer));
    }

    private int GetSortingOrderForNewbie(char layer)
    {
        if (!layerGameObject.ContainsKey(layer))
        {
            return 0;
        }
        return layerGameObject[layer].transform.childCount;
    }

    private void HandleAnimation(ScriptCommand<GadgetOpcode> command)
    {
        if (command.Arguments.Count < 4)
        {
            Debug.LogError("Not enough argument for animation creation!");
            return;
        }

        string animFileName = command.Arguments[1] as string;
        char layer = (command.Arguments[0] as string)[0];

        GameObject layerObject = ParentByLayer(layer);
        GameObject existingAnim = layerObject.transform.Find(animFileName)?.gameObject;
        Vector2 position = new Vector2(int.Parse(command.Arguments[2] as string), int.Parse(command.Arguments[3] as string));

        if (existingAnim != null)
        {
            SpriteAnimatorController existingController = existingAnim.GetComponent<SpriteAnimatorController>();
            if (existingController != null)
            {
                existingController.Restart(position);
            }

            return;
        }

        GameObject animObject = GameObject.Instantiate(GameManager.Instance.gameAnimationPrefabObject, layerObject.transform, false);
        animObject.name = animFileName;

        SpriteAnimatorController controller = animObject.GetComponent<SpriteAnimatorController>();
        if (controller != null)
        {
            controller.Setup(position, GetSortingOrderForNewbie(layer), animFileName, GetSortingLayer(layer));
        }
    }

    private void HandleImage(ScriptCommand<GadgetOpcode> command)
    {
        if (command.Arguments.Count < 2)
        {
            Debug.LogError("Not enough argument for image creation!");
            return;
        }

        char layer = (command.Arguments[0] as string)[0];

        GameObject imageObject = GameObject.Instantiate(GameManager.Instance.gameImagePrefabObject, ParentByLayer(layer).transform, false);
        imageObject.transform.localPosition = Vector3.zero;
        
        SpriteRenderer spriteRenderer = imageObject.GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            string path = command.Arguments[1] as string;
            spriteRenderer.sprite = SpriteManager.Instance.Load(ResourceManager.Instance.PickBestResourcePackForFile(path), path);
            spriteRenderer.sortingOrder = GetSortingOrderForNewbie(layer);
            spriteRenderer.sortingLayerName = GetSortingLayer(layer);
        }
    }

    private void HandlePlay(ScriptCommand<GadgetOpcode> command)
    {
        if (command.Arguments.Count < 2)
        {
            Debug.LogError("Not enough argument for sound play request!");
            return;
        }

        GameManager.Instance.PlayAudioInDialogue(command.Arguments[1] as string, command.Arguments[0] as string);
    }

    private void HandleText(ScriptCommand<GadgetOpcode> command)
    {
        string textId = command.Arguments[0] as string;

        if (!dialogue.Strings.TryGetValue(textId, out string text)) {
            text = textId;
        }

        GameManager.Instance.ShowText(text, command.Arguments[1] as string);
    }

    private void HandleHideText(ScriptCommand<GadgetOpcode> command)
    {
        GameManager.Instance.HideText(command.Arguments[0] as string);
    }

    private void HandleBalloon(ScriptCommand<GadgetOpcode> command)
    {
        GameManager.Instance.SetBalloon(command.Arguments[0] as string, command.Arguments[1] as string);
    }

    private void HandleStinger(ScriptCommand<GadgetOpcode> command)
    {
        Vector2 position = new Vector2(int.Parse(command.Arguments[0] as string), int.Parse(command.Arguments[1] as string));
        string filename = command.Arguments[2] as string;
        string where = command.Arguments[3] as string;

        GameManager.Instance.SetStinger(position, filename, where);
    }

    private void HandleIcon(ScriptCommand<GadgetOpcode> command)
    {
        Vector2 position = new Vector2(int.Parse(command.Arguments[1] as string), int.Parse(command.Arguments[2] as string));
        string iconName = command.Arguments[0] as string;

        GameManager.Instance.DisplayIcon(iconName, position);
    }

    private void HandleClear(ScriptCommand<GadgetOpcode> command)
    {
        char layer = (command.Arguments[0] as string)[0];
        if (layerGameObject.ContainsKey(layer))
        {
            GameObject layerObj = layerGameObject[layer];

            // We are running in coroutine! So destroy like this is fine...
            for (int i = 0; i < layerObj.transform.childCount; i++)
            {
                GameObject.Destroy(layerObj.transform.GetChild(i).gameObject);
            }

            layerObj.transform.localPosition = Vector3.zero;
        }
    }

    private IEnumerator StartAction(ScriptCommand<GadgetOpcode> command)
    {
        if (command.Arguments.Count == 0)
        {
            Debug.LogWarning("Empty action to execute!");
            yield break;
        }

        yield return actionInterpreter.Execute(command.Arguments[0] as ScriptBlock<ActionOpcode>);
    }

    private void HandlePan(ScriptCommand<GadgetOpcode> command)
    {
        if (command.Arguments.Count < 4)
        {
            Debug.LogError("Not enough argument for panning!");
            return;
        }

        char layer = (command.Arguments[0] as string)[0];
        GameObject layerObj = ParentByLayer(layer);

        if (layerObj != null) {
            Vector2 target = new Vector2(int.Parse(command.Arguments[1] as string), int.Parse(command.Arguments[2] as string));
            target = GameUtils.ToUnityCoordinates(target);

            int duration = int.Parse(command.Arguments[3] as string);

            GamePanController panController = layerObj.GetComponent<GamePanController>();
            if (panController != null)
            {
                panController.Pan(target, duration);
            }
        }
    }

    private void HandleFade(ScriptCommand<GadgetOpcode> command, bool isFadeIn)
    {
        if (command.Arguments.Count < 2)
        {
            Debug.LogError("Not enough argument for fade!");
            return;
        }

        char layer = (command.Arguments[0] as string)[0];
        GameObject layerObj = ParentByLayer(layer);

        if (layerObj != null)
        {
            int frames = int.Parse(command.Arguments[1] as string);

            for (int i = 0; i < layerObj.transform.childCount; i++)
            {
                GameObject individualObj = layerObj.transform.GetChild(i).gameObject;
                GameImageFadeController fadeController = individualObj.GetComponent<GameImageFadeController>();
                
                if (fadeController != null)
                {
                    fadeController.Fade(isFadeIn, frames);
                }
            }
        }
    }

    private void HandleChoice(ScriptCommand<GadgetOpcode> command)
    {
        if (command.Arguments.Count == 0)
        {
            Debug.LogError("Invalid number of argument for choices!");
        }

        List<GadgetChoiceInfo> choices = command.Arguments[0] as List<GadgetChoiceInfo>;
        List<Tuple<string, int>> finalDisplayChoices = new List<Tuple<string, int>>();
    
        foreach (var choice in choices)
        {
            if (!actionInterpreter.GUIConditionResult(choice.ConditionalVariables, choice.ConditionalVariableValues))
            {
                continue;
            }

            if (!dialogue.Strings.TryGetValue(choice.TextId, out string text))
            {
                text = choice.TextId;
            }

            finalDisplayChoices.Add(new Tuple<string, int>(text, choice.DialogueId));
        }

        GameManager.Instance.AddChoices(dialogue, finalDisplayChoices);
    }

    private void HandleContinue(ScriptCommand<GadgetOpcode> command)
    {
        if (command.Arguments.Count == 0)
        {
            Debug.LogError("Not enough argument for continue dialoging!");
            return;
        }

        int id = int.Parse(command.Arguments[0] as string);
        DialogueSlide slide = dialogue.GetDialogueSlideWithId(id);

        if (slide == null)
        {
            Debug.LogError("Invalid dialogue slide id " + id + " in file: " + dialogue.FileName);
        }

        GameManager.Instance.LoadDialogueSlide(dialogue, slide);
    }

    public IEnumerator Execute()
    {
        if (GameManager.Instance.GUIBusy)
        {
            yield return new WaitUntil(() => !GameManager.Instance.GUIBusy);
        }

        foreach (var command in scriptBlock.Commands)
        {
            switch (command.Opcode)
            {
                case GadgetOpcode.Pause:
                    {
                        int frameToPause = int.Parse(command.Arguments[0] as string);
                        if (frameToPause < 0)
                        {
                            // Wait for confirmation basically!
                            yield return new WaitUntil(() => GameManager.Instance.LastTextFinished);
                            GameManager.Instance.StartHearingConfirmation();
                            yield return new WaitUntil(() => GameManager.Instance.ContinueConfirmed);
                        } else
                        {
                            for (int i = 0; i < frameToPause; i++)
                            {
                                yield return null;
                            }
                        }

                        break;
                    }

                case GadgetOpcode.Pan:
                    HandlePan(command);
                    break;

                case GadgetOpcode.FadeIn:
                case GadgetOpcode.FadeOut:
                    HandleFade(command, (command.Opcode == GadgetOpcode.FadeIn));
                    break;

                case GadgetOpcode.Load:
                    HandleImage(command);
                    break;

                case GadgetOpcode.Animation:
                    HandleAnimation(command);
                    break;

                case GadgetOpcode.Clear:
                    HandleClear(command);
                    break;

                case GadgetOpcode.StartAction:
                    yield return StartAction(command);
                    break;

                case GadgetOpcode.Continue:
                    yield return new WaitUntil(() => GameManager.Instance.LastTextFinished);
                    GameManager.Instance.StartHearingConfirmation();
                    yield return new WaitUntil(() => GameManager.Instance.ContinueConfirmed);

                    HandleContinue(command);
                    break;

                case GadgetOpcode.Play:
                    HandlePlay(command);
                    break;

                case GadgetOpcode.Text:
                    HandleText(command);
                    break;

                case GadgetOpcode.HideText:
                    HandleHideText(command);
                    break;

                case GadgetOpcode.Balloon:
                    HandleBalloon(command);
                    break;

                case GadgetOpcode.Stinger:
                    HandleStinger(command);
                    break;

                case GadgetOpcode.Icon:
                    HandleIcon(command);
                    break;

                case GadgetOpcode.Choice:
                    HandleChoice(command);
                    break;

                default:
                    Debug.LogError("Unhandled gadget opcode " + command.Opcode);
                    break;
            }
        }

        yield break;
    }
};