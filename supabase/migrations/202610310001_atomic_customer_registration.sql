-- Both inserts execute in the RPC transaction; any failure rolls back both.
create or replace function public.register_customer(
    p_email text,
    p_full_name text,
    p_password_hash text
)
returns table (id_khachhang integer)
language plpgsql
security invoker
set search_path = ''
as $$
declare
    customer_id public.khachhang.id_khachhang%type;
    normalized_email text := btrim(p_email);
begin
    if normalized_email is null or normalized_email = '' or
       p_full_name is null or btrim(p_full_name) = '' or
       p_password_hash is null or p_password_hash = '' then
        raise exception 'Thông tin đăng ký không được để trống.' using errcode = '22023';
    end if;

    -- Serialize registrations for the same email, including concurrent RPC calls.
    perform pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(normalized_email, 0));
    if exists (select 1 from public.khachhang k where k.email = normalized_email) or
       exists (select 1 from public.dangnhap d where d.tendangnhap = normalized_email) then
        raise exception 'Email này đã được đăng ký. Hãy đăng nhập hoặc dùng email khác.'
            using errcode = '23505';
    end if;

    insert into public.khachhang as k (hoten, email)
    values (btrim(p_full_name), normalized_email)
    returning k.id_khachhang into customer_id;

    insert into public.dangnhap (tendangnhap, loaitaikhoan, matkhau, id_khachhang, trangthai)
    values (normalized_email, 'KHACHHANG', p_password_hash, customer_id, 1);

    return query select customer_id;
end;
$$;

revoke all on function public.register_customer(text, text, text) from public;
grant execute on function public.register_customer(text, text, text) to anon, authenticated;
notify pgrst, 'reload schema';
