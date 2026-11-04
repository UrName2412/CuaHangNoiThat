create or replace function public.update_customer_profile(
	p_customer_id integer,
	p_full_name text,
	p_email text,
	p_phone text
)
returns void
language plpgsql
security invoker
set search_path = ''
as $$
declare
	normalized_email text := btrim(p_email);
	normalized_name text := btrim(p_full_name);
	normalized_phone text := btrim(coalesce(p_phone, ''));
begin
	if p_customer_id is null or p_customer_id <= 0 then
		raise exception 'Khách hàng không hợp lệ.' using errcode = '22023';
	end if;

	if normalized_email is null or normalized_email = '' or normalized_name is null or normalized_name = '' then
		raise exception 'Họ tên và email không được để trống.' using errcode = '22023';
	end if;

	if not exists (
		select 1 from public.khachhang k where k.id_khachhang = p_customer_id
	) or not exists (
		select 1 from public.dangnhap d
		where d.id_khachhang = p_customer_id
		  and d.loaitaikhoan = 'KHACHHANG'
		  and d.trangthai = 1
	) then
		raise exception 'Không tìm thấy hồ sơ khách hàng hợp lệ.' using errcode = 'P0002';
	end if;

	perform pg_catalog.pg_advisory_xact_lock(pg_catalog.hashtextextended(normalized_email, 0));

	if exists (
		select 1
		from public.khachhang k
		where k.email = normalized_email
		  and k.id_khachhang <> p_customer_id
	) or exists (
		select 1
		from public.dangnhap d
		where d.tendangnhap = normalized_email
		  and d.loaitaikhoan = 'KHACHHANG'
		  and d.id_khachhang <> p_customer_id
	) then
		raise exception 'Email này đã được đăng ký. Hãy dùng email khác.' using errcode = '23505';
	end if;

	update public.khachhang
	set hoten = normalized_name,
		email = normalized_email,
		sdt = normalized_phone
	where id_khachhang = p_customer_id;

	update public.dangnhap
	set tendangnhap = normalized_email
	where id_khachhang = p_customer_id
	  and loaitaikhoan = 'KHACHHANG'
	  and trangthai = 1;
end;
$$;

revoke all on function public.update_customer_profile(integer, text, text, text) from public;
grant execute on function public.update_customer_profile(integer, text, text, text) to anon, authenticated;
notify pgrst, 'reload schema';
