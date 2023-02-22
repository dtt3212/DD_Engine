﻿using UnityEngine;

public class GUIMenuItemController : GUIMenuSelectableBehaviour
{
    private GUIControlSet ownSet;
    private TMPro.TMP_Text text;
    private string textId;

    private void UpdateText()
    {
        text.text = ownSet.GetLanguageString(textId);
        text.font = ResourceManager.Instance.GetFontAssetForLocalization();
    }

    public void Setup(GUIControlSet ownSet, GUIControlMenuItemDescription description, ref Vector2 positonBase)
    {
        this.ownSet = ownSet;
        this.textId = description.TextName;
        this.text = GetComponentInChildren<TMPro.TMP_Text>();

        if (description.Id != null)
        {
            this.name = description.Id;
        }

        transform.localPosition = GameUtils.ToUnityCoordinates(description.Position) + positonBase;

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = GameUtils.ToUnitySortingPosition(description.AbsoluteDepth);
            renderer.sprite = SpriteManager.Instance.Load(ResourceManager.Instance.GeneralResources, description.ImagePath);

            positonBase += renderer.sprite.bounds.size * Vector2.down;
            Vector2 sizeReal = renderer.sprite.textureRect.size / Constants.PixelsPerUnit;

            // Adjust the rect so that text can properly render
            RectTransform transform = GetComponent<RectTransform>();
            if (transform)
            {
                transform.sizeDelta = sizeReal;
            }

            text.rectTransform.sizeDelta = sizeReal;

            var meshRenderer = text.GetComponent<MeshRenderer>();
            meshRenderer.sortingOrder = GameUtils.ToUnitySortingPosition(description.AbsoluteDepth);

            UpdateText();
        }
    }
}