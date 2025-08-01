PGDMP      6                }            enrollment_db    17.2    17.2 -    _           0    0    ENCODING    ENCODING        SET client_encoding = 'UTF8';
                           false            `           0    0 
   STDSTRINGS 
   STDSTRINGS     (   SET standard_conforming_strings = 'on';
                           false            a           0    0 
   SEARCHPATH 
   SEARCHPATH     8   SELECT pg_catalog.set_config('search_path', '', false);
                           false            b           1262    20079    enrollment_db    DATABASE     �   CREATE DATABASE enrollment_db WITH TEMPLATE = template0 ENCODING = 'UTF8' LOCALE_PROVIDER = libc LOCALE = 'English_Philippines.1252';
    DROP DATABASE enrollment_db;
                     postgres    false            �            1259    20136    academic_year    TABLE     �   CREATE TABLE public.academic_year (
    ay_code character varying(20) NOT NULL,
    ay_start_year integer NOT NULL,
    ay_end_year integer NOT NULL,
    CONSTRAINT valid_year_range CHECK ((ay_end_year = (ay_start_year + 1)))
);
 !   DROP TABLE public.academic_year;
       public         heap r       postgres    false            �            1259    20154    block_section    TABLE     .  CREATE TABLE public.block_section (
    bsec_code character varying(20) NOT NULL,
    bsec_name character varying(100) NOT NULL,
    bsec_status character varying(20) DEFAULT true,
    prog_code character varying(50) NOT NULL,
    ay_code character varying(20) NOT NULL,
    sem_id integer NOT NULL
);
 !   DROP TABLE public.block_section;
       public         heap r       postgres    false            �            1259    20131    course_category    TABLE     �   CREATE TABLE public.course_category (
    ctg_code character varying(50) NOT NULL,
    ctg_name character varying(100) NOT NULL
);
 #   DROP TABLE public.course_category;
       public         heap r       postgres    false            �            1259    20118    faculty    TABLE     $  CREATE TABLE public.faculty (
    id integer NOT NULL,
    fullname character varying(150) NOT NULL,
    username character varying(100) NOT NULL,
    password text NOT NULL,
    isprofessor boolean DEFAULT false,
    isadmin boolean DEFAULT false,
    isprogramhead boolean DEFAULT false
);
    DROP TABLE public.faculty;
       public         heap r       postgres    false            �            1259    20117    faculty_id_seq    SEQUENCE     �   CREATE SEQUENCE public.faculty_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;
 %   DROP SEQUENCE public.faculty_id_seq;
       public               postgres    false    218            c           0    0    faculty_id_seq    SEQUENCE OWNED BY     A   ALTER SEQUENCE public.faculty_id_seq OWNED BY public.faculty.id;
          public               postgres    false    217            �            1259    20149    program    TABLE     ~   CREATE TABLE public.program (
    prog_code character varying(50) NOT NULL,
    prog_title character varying(100) NOT NULL
);
    DROP TABLE public.program;
       public         heap r       postgres    false            �            1259    20143    semester    TABLE     k   CREATE TABLE public.semester (
    sem_id integer NOT NULL,
    sem_name character varying(50) NOT NULL
);
    DROP TABLE public.semester;
       public         heap r       postgres    false            �            1259    20142    semester_sem_id_seq    SEQUENCE     �   CREATE SEQUENCE public.semester_sem_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;
 *   DROP SEQUENCE public.semester_sem_id_seq;
       public               postgres    false    222            d           0    0    semester_sem_id_seq    SEQUENCE OWNED BY     K   ALTER SEQUENCE public.semester_sem_id_seq OWNED BY public.semester.sem_id;
          public               postgres    false    221            �            1259    20176    student    TABLE     �  CREATE TABLE public.student (
    stud_id integer NOT NULL,
    stud_fname character varying(50) NOT NULL,
    stud_lname character varying(50) NOT NULL,
    stud_mname character varying(50),
    stud_email character varying(100) NOT NULL,
    stud_code integer NOT NULL,
    stud_dob date NOT NULL,
    stud_contact character varying(20) NOT NULL,
    stud_address text,
    stud_district character varying(50),
    stud_is_first_gen boolean DEFAULT false,
    stud_yr_level integer,
    stud_major character varying(100),
    stud_status character varying(20),
    stud_sem integer,
    bsec_code character varying(20),
    prog_code character varying(50),
    stud_password character varying(255) NOT NULL
);
    DROP TABLE public.student;
       public         heap r       postgres    false            �            1259    20175    student_stud_id_seq    SEQUENCE     �   CREATE SEQUENCE public.student_stud_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;
 *   DROP SEQUENCE public.student_stud_id_seq;
       public               postgres    false    226            e           0    0    student_stud_id_seq    SEQUENCE OWNED BY     K   ALTER SEQUENCE public.student_stud_id_seq OWNED BY public.student.stud_id;
          public               postgres    false    225            �           2604    20121 
   faculty id    DEFAULT     h   ALTER TABLE ONLY public.faculty ALTER COLUMN id SET DEFAULT nextval('public.faculty_id_seq'::regclass);
 9   ALTER TABLE public.faculty ALTER COLUMN id DROP DEFAULT;
       public               postgres    false    218    217    218            �           2604    20146    semester sem_id    DEFAULT     r   ALTER TABLE ONLY public.semester ALTER COLUMN sem_id SET DEFAULT nextval('public.semester_sem_id_seq'::regclass);
 >   ALTER TABLE public.semester ALTER COLUMN sem_id DROP DEFAULT;
       public               postgres    false    221    222    222            �           2604    20179    student stud_id    DEFAULT     r   ALTER TABLE ONLY public.student ALTER COLUMN stud_id SET DEFAULT nextval('public.student_stud_id_seq'::regclass);
 >   ALTER TABLE public.student ALTER COLUMN stud_id DROP DEFAULT;
       public               postgres    false    226    225    226            V          0    20136    academic_year 
   TABLE DATA           L   COPY public.academic_year (ay_code, ay_start_year, ay_end_year) FROM stdin;
    public               postgres    false    220   �8       Z          0    20154    block_section 
   TABLE DATA           f   COPY public.block_section (bsec_code, bsec_name, bsec_status, prog_code, ay_code, sem_id) FROM stdin;
    public               postgres    false    224   �8       U          0    20131    course_category 
   TABLE DATA           =   COPY public.course_category (ctg_code, ctg_name) FROM stdin;
    public               postgres    false    219   �8       T          0    20118    faculty 
   TABLE DATA           h   COPY public.faculty (id, fullname, username, password, isprofessor, isadmin, isprogramhead) FROM stdin;
    public               postgres    false    218   �8       Y          0    20149    program 
   TABLE DATA           8   COPY public.program (prog_code, prog_title) FROM stdin;
    public               postgres    false    223   �9       X          0    20143    semester 
   TABLE DATA           4   COPY public.semester (sem_id, sem_name) FROM stdin;
    public               postgres    false    222   �9       \          0    20176    student 
   TABLE DATA             COPY public.student (stud_id, stud_fname, stud_lname, stud_mname, stud_email, stud_code, stud_dob, stud_contact, stud_address, stud_district, stud_is_first_gen, stud_yr_level, stud_major, stud_status, stud_sem, bsec_code, prog_code, stud_password) FROM stdin;
    public               postgres    false    226   �9       f           0    0    faculty_id_seq    SEQUENCE SET     <   SELECT pg_catalog.setval('public.faculty_id_seq', 3, true);
          public               postgres    false    217            g           0    0    semester_sem_id_seq    SEQUENCE SET     B   SELECT pg_catalog.setval('public.semester_sem_id_seq', 1, false);
          public               postgres    false    221            h           0    0    student_stud_id_seq    SEQUENCE SET     A   SELECT pg_catalog.setval('public.student_stud_id_seq', 1, true);
          public               postgres    false    225            �           2606    20141     academic_year academic_year_pkey 
   CONSTRAINT     c   ALTER TABLE ONLY public.academic_year
    ADD CONSTRAINT academic_year_pkey PRIMARY KEY (ay_code);
 J   ALTER TABLE ONLY public.academic_year DROP CONSTRAINT academic_year_pkey;
       public                 postgres    false    220            �           2606    20159     block_section block_section_pkey 
   CONSTRAINT     e   ALTER TABLE ONLY public.block_section
    ADD CONSTRAINT block_section_pkey PRIMARY KEY (bsec_code);
 J   ALTER TABLE ONLY public.block_section DROP CONSTRAINT block_section_pkey;
       public                 postgres    false    224            �           2606    20135 $   course_category course_category_pkey 
   CONSTRAINT     h   ALTER TABLE ONLY public.course_category
    ADD CONSTRAINT course_category_pkey PRIMARY KEY (ctg_code);
 N   ALTER TABLE ONLY public.course_category DROP CONSTRAINT course_category_pkey;
       public                 postgres    false    219            �           2606    20128    faculty faculty_pkey 
   CONSTRAINT     R   ALTER TABLE ONLY public.faculty
    ADD CONSTRAINT faculty_pkey PRIMARY KEY (id);
 >   ALTER TABLE ONLY public.faculty DROP CONSTRAINT faculty_pkey;
       public                 postgres    false    218            �           2606    20130    faculty faculty_username_key 
   CONSTRAINT     [   ALTER TABLE ONLY public.faculty
    ADD CONSTRAINT faculty_username_key UNIQUE (username);
 F   ALTER TABLE ONLY public.faculty DROP CONSTRAINT faculty_username_key;
       public                 postgres    false    218            �           2606    20153    program program_pkey 
   CONSTRAINT     Y   ALTER TABLE ONLY public.program
    ADD CONSTRAINT program_pkey PRIMARY KEY (prog_code);
 >   ALTER TABLE ONLY public.program DROP CONSTRAINT program_pkey;
       public                 postgres    false    223            �           2606    20148    semester semester_pkey 
   CONSTRAINT     X   ALTER TABLE ONLY public.semester
    ADD CONSTRAINT semester_pkey PRIMARY KEY (sem_id);
 @   ALTER TABLE ONLY public.semester DROP CONSTRAINT semester_pkey;
       public                 postgres    false    222            �           2606    20184    student student_pkey 
   CONSTRAINT     W   ALTER TABLE ONLY public.student
    ADD CONSTRAINT student_pkey PRIMARY KEY (stud_id);
 >   ALTER TABLE ONLY public.student DROP CONSTRAINT student_pkey;
       public                 postgres    false    226            �           2606    20188    student student_stud_code_key 
   CONSTRAINT     ]   ALTER TABLE ONLY public.student
    ADD CONSTRAINT student_stud_code_key UNIQUE (stud_code);
 G   ALTER TABLE ONLY public.student DROP CONSTRAINT student_stud_code_key;
       public                 postgres    false    226            �           2606    20186    student student_stud_email_key 
   CONSTRAINT     _   ALTER TABLE ONLY public.student
    ADD CONSTRAINT student_stud_email_key UNIQUE (stud_email);
 H   ALTER TABLE ONLY public.student DROP CONSTRAINT student_stud_email_key;
       public                 postgres    false    226            �           2606    20165 (   block_section block_section_ay_code_fkey    FK CONSTRAINT     �   ALTER TABLE ONLY public.block_section
    ADD CONSTRAINT block_section_ay_code_fkey FOREIGN KEY (ay_code) REFERENCES public.academic_year(ay_code);
 R   ALTER TABLE ONLY public.block_section DROP CONSTRAINT block_section_ay_code_fkey;
       public               postgres    false    224    220    4784            �           2606    20160 *   block_section block_section_prog_code_fkey    FK CONSTRAINT     �   ALTER TABLE ONLY public.block_section
    ADD CONSTRAINT block_section_prog_code_fkey FOREIGN KEY (prog_code) REFERENCES public.program(prog_code);
 T   ALTER TABLE ONLY public.block_section DROP CONSTRAINT block_section_prog_code_fkey;
       public               postgres    false    4788    224    223            �           2606    20170 '   block_section block_section_sem_id_fkey    FK CONSTRAINT     �   ALTER TABLE ONLY public.block_section
    ADD CONSTRAINT block_section_sem_id_fkey FOREIGN KEY (sem_id) REFERENCES public.semester(sem_id);
 Q   ALTER TABLE ONLY public.block_section DROP CONSTRAINT block_section_sem_id_fkey;
       public               postgres    false    224    4786    222            �           2606    20189    student student_bsec_code_fkey    FK CONSTRAINT     �   ALTER TABLE ONLY public.student
    ADD CONSTRAINT student_bsec_code_fkey FOREIGN KEY (bsec_code) REFERENCES public.block_section(bsec_code) ON UPDATE CASCADE ON DELETE CASCADE;
 H   ALTER TABLE ONLY public.student DROP CONSTRAINT student_bsec_code_fkey;
       public               postgres    false    224    4790    226            �           2606    20194    student student_prog_code_fkey    FK CONSTRAINT     �   ALTER TABLE ONLY public.student
    ADD CONSTRAINT student_prog_code_fkey FOREIGN KEY (prog_code) REFERENCES public.program(prog_code) ON UPDATE CASCADE ON DELETE CASCADE;
 H   ALTER TABLE ONLY public.student DROP CONSTRAINT student_prog_code_fkey;
       public               postgres    false    4788    223    226            V      x������ � �      Z      x������ � �      U      x������ � �      T   �   x��ϻ�0����Ut`n8L�Xb�Ԩ�˟�U������g��|M�ݎ���Rn�>kk�������0~L�ϛ�ܳ<-���U<��j���|F�}�L�|�d�� (P$�X���;4�R�d脵N�%���cOt��'�C��K�`����6@��Ư\Q      Y      x������ � �      X      x������ � �      \   �   x�]�=�0 ���`��Q݌:������B���������Pвj�6�j@hٵ=�ί�.��E�=�v�(NdGD�Yӎ���B���@�s����SZ���G=
ӻ͗W����.�Y�or~4T0j?6�ʝ�w�"|/�     