/// <summary>
/// 데미지를 받을 수 있는 대상이 구현하는 인터페이스.
/// 몬스터, 파괴 가능한 오브젝트 등에서 구현 예정.
/// </summary>
public interface IDamageable
{
    void TakeDamage(float amount);
}
