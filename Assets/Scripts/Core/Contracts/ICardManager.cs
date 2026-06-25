// 손패 입력·정렬·배치·스킬 시전·필드 미리보기·딜 예측 (CardManager 구현)
public interface ICardManager
{
    bool IsDamagePreviewActive { get; }

    void CardMouseOver(Card card);
    void CardMouseExit(Card card);
    void CardMouseDown();
    void CardMouseUp();

    void SkillCardMouseOver(SkillCard card);
    void SkillCardMouseExit(SkillCard card);
    void SkillCardMouseDown(SkillCard card);
    void SkillCardMouseUp(SkillCard card);

    void ShowFieldPreview(Entity entity);
    void HideFieldPreview(Entity entity);

    void ShowDamagePreview(Entity attacker, Entity defender);
    void HideDamagePreview();
    void ShowWarning(string message);

    void DrawSkillCards(bool isMine, int count);
    bool TryPutCard(bool isMine, bool isFrontRow);
    void CheatAutoPlaceMyCards();
    bool TryCastOtherSkill();
}
