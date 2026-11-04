create or replace function public.set_default_address(
	p_customer_id integer,
	p_address_id integer
)
returns void
language plpgsql
security invoker
set search_path = ''
as $$
begin
	if p_customer_id is null or p_customer_id <= 0 or p_address_id is null or p_address_id <= 0 then
		raise exception 'Thông tin địa chỉ không hợp lệ.' using errcode = '22023';
	end if;

	if not exists (
		select 1
		from public.diachigiaohang d
		where d.id_khachhang = p_customer_id
		  and d.id_diachi = p_address_id
	) then
		raise exception 'Địa chỉ không tồn tại hoặc không thuộc khách hàng hiện tại.' using errcode = 'P0002';
	end if;

	update public.diachigiaohang
	set macdinh = case when id_diachi = p_address_id then 1 else 0 end
	where id_khachhang = p_customer_id;
end;
$$;

revoke all on function public.set_default_address(integer, integer) from public;
grant execute on function public.set_default_address(integer, integer) to anon, authenticated;
notify pgrst, 'reload schema';
